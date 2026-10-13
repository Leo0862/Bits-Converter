namespace BitsConverter.Core;

/// <summary>
/// A contiguous run of bits that either belongs to one field or is unassigned (<see cref="Field"/> is null).
/// For a field that extends past the current width, the segment is clipped to the width.
/// </summary>
public sealed record FieldSegment(int Msb, int Lsb, BitField? Field)
{
    public bool IsUnassigned => Field is null;

    public bool Contains(int bit) => bit <= Msb && bit >= Lsb;
}

public static class FieldLayout
{
    private static readonly char[] ReservedNameChars = [',', ';', '=', ':', '[', ']', '\r', '\n'];

    public static bool IsValidName(string? name) =>
        name is null || name.IndexOfAny(ReservedNameChars) < 0;

    /// <summary>Returns the fields ordered from the most significant to the least significant.</summary>
    public static IReadOnlyList<BitField> Sort(IEnumerable<BitField> fields) =>
        fields.OrderByDescending(f => f.Msb).ToList();

    public static bool TryValidate(IEnumerable<BitField> fields, out string error)
    {
        var sorted = Sort(fields);
        for (var i = 0; i < sorted.Count; i++)
        {
            var f = sorted[i];
            if (f.Lsb < 0 || f.Msb >= BitMath.MaxWidth)
            {
                error = $"{f.RangeText}: bit index must be between 0 and {BitMath.MaxWidth - 1}";
                return false;
            }
            if (f.Msb < f.Lsb)
            {
                error = $"{f.RangeText}: MSB must not be lower than LSB";
                return false;
            }
            if (!IsValidName(f.Name))
            {
                error = $"{f.RangeText}: name must not contain , ; = : [ ]";
                return false;
            }
            if (i > 0 && sorted[i - 1].Overlaps(f))
            {
                error = $"{sorted[i - 1].RangeText} overlaps {f.RangeText}";
                return false;
            }
        }
        error = string.Empty;
        return true;
    }

    /// <summary>Splits bits [width-1:0] into field segments and unassigned runs, MSB first.</summary>
    public static IReadOnlyList<FieldSegment> GetSegments(IEnumerable<BitField> fields, int width)
    {
        var sorted = Sort(fields);
        var result = new List<FieldSegment>();
        var bit = width - 1;
        while (bit >= 0)
        {
            var owner = sorted.FirstOrDefault(f => f.Contains(bit));
            if (owner is not null)
            {
                result.Add(new FieldSegment(bit, owner.Lsb, owner));
                bit = owner.Lsb - 1;
                continue;
            }

            // Extend the unassigned run down to the next owned bit.
            var lsb = bit;
            while (lsb - 1 >= 0 && !sorted.Any(f => f.Contains(lsb - 1)))
                lsb--;
            result.Add(new FieldSegment(bit, lsb, null));
            bit = lsb - 1;
        }
        return result;
    }

    public static FieldSegment SegmentOf(IReadOnlyList<FieldSegment> segments, int bit) =>
        segments.First(s => s.Contains(bit));

    /// <summary>True when bits <paramref name="upperBit"/> and upperBit-1 belong to different segments.</summary>
    public static bool IsBoundary(IReadOnlyList<FieldSegment> segments, int upperBit) =>
        SegmentOf(segments, upperBit) != SegmentOf(segments, upperBit - 1);

    /// <summary>
    /// A boundary can be toggled unless one side belongs to a field that does not fit the width,
    /// since editing it could create overlaps once the width grows again.
    /// </summary>
    public static bool CanToggleBoundary(IReadOnlyList<FieldSegment> segments, int upperBit, int width)
    {
        if (upperBit <= 0 || upperBit >= width) return false;
        var hi = SegmentOf(segments, upperBit);
        var lo = SegmentOf(segments, upperBit - 1);
        return hi.Field?.IsInRange(width) != false && lo.Field?.IsInRange(width) != false;
    }

    /// <summary>
    /// Splits the segment that spans bits upperBit and upperBit-1, or merges the two segments
    /// that meet there. Returns the new field list, or the original list if the toggle is not allowed.
    /// </summary>
    public static IReadOnlyList<BitField> ToggleBoundary(IEnumerable<BitField> fields, int width, int upperBit)
    {
        var list = Sort(fields).ToList();
        var segments = GetSegments(list, width);
        if (!CanToggleBoundary(segments, upperBit, width)) return list;

        var hi = SegmentOf(segments, upperBit);
        var lo = SegmentOf(segments, upperBit - 1);

        if (hi == lo)
        {
            // Split
            if (hi.Field is { } f)
            {
                list.Remove(f);
                list.Add(new BitField(f.Msb, upperBit, f.Name));
                list.Add(new BitField(upperBit - 1, f.Lsb));
            }
            else
            {
                list.Add(new BitField(hi.Msb, upperBit));
                list.Add(new BitField(upperBit - 1, hi.Lsb));
            }
        }
        else
        {
            // Merge
            var name = !string.IsNullOrEmpty(hi.Field?.Name) ? hi.Field!.Name : lo.Field?.Name ?? string.Empty;
            if (hi.Field is not null) list.Remove(hi.Field);
            if (lo.Field is not null) list.Remove(lo.Field);
            list.Add(new BitField(hi.Msb, lo.Lsb, name));
        }

        return Sort(list);
    }
}
