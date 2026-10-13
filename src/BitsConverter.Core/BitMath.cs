namespace BitsConverter.Core;

/// <summary>
/// Bit manipulation helpers for values up to 32 bits wide.
/// </summary>
public static class BitMath
{
    public const int MaxWidth = 32;

    public static readonly IReadOnlyList<int> SupportedWidths = [8, 16, 24, 32];

    public static bool IsSupportedWidth(int width) => SupportedWidths.Contains(width);

    /// <summary>Returns a mask with the lowest <paramref name="width"/> bits set.</summary>
    public static uint Mask(int width)
    {
        if (width is < 0 or > MaxWidth)
            throw new ArgumentOutOfRangeException(nameof(width));
        return width == MaxWidth ? uint.MaxValue : (1u << width) - 1;
    }

    public static uint MaxValue(int width) => Mask(width);

    public static bool GetBit(uint value, int index) => ((value >> index) & 1u) != 0;

    public static uint SetBit(uint value, int index, bool on) =>
        on ? value | (1u << index) : value & ~(1u << index);

    public static uint Invert(uint value, int width) => ~value & Mask(width);

    /// <summary>Logical shift left; bits shifted past the width are discarded.</summary>
    public static uint ShiftLeft(uint value, int count, int width) =>
        count >= width ? 0u : (value << count) & Mask(width);

    /// <summary>Logical shift right; zeros are shifted in.</summary>
    public static uint ShiftRight(uint value, int count, int width) =>
        count >= width ? 0u : (value & Mask(width)) >> count;

    public static uint RotateLeft(uint value, int count, int width)
    {
        value &= Mask(width);
        count %= width;
        if (count == 0) return value;
        return ((value << count) | (value >> (width - count))) & Mask(width);
    }

    public static uint RotateRight(uint value, int count, int width)
    {
        count %= width;
        return RotateLeft(value, (width - count) % width, width);
    }

    public static uint FieldMask(int msb, int lsb) => Mask(msb - lsb + 1) << lsb;

    public static uint GetField(uint value, int msb, int lsb) =>
        (value >> lsb) & Mask(msb - lsb + 1);

    /// <summary>Replaces bits [msb:lsb] of <paramref name="value"/> with <paramref name="fieldValue"/>.</summary>
    public static uint SetField(uint value, int msb, int lsb, uint fieldValue)
    {
        var mask = FieldMask(msb, lsb);
        return (value & ~mask) | ((fieldValue << lsb) & mask);
    }
}
