namespace BitsConverter.Core;

/// <summary>A named bit range [Msb:Lsb] inside a value.</summary>
public sealed record BitField(int Msb, int Lsb, string Name = "")
{
    public int Width => Msb - Lsb + 1;

    public bool Contains(int bit) => bit <= Msb && bit >= Lsb;

    public bool Overlaps(BitField other) => Lsb <= other.Msb && other.Lsb <= Msb;

    /// <summary>True when the whole field fits inside a value of the given width.</summary>
    public bool IsInRange(int width) => Msb < width;

    public string RangeText => Msb == Lsb ? $"[{Msb}]" : $"[{Msb}:{Lsb}]";

    public override string ToString() => Name + RangeText;
}
