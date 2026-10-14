using System.Globalization;
using System.Text;

namespace BitsConverter.Core;

public static class NumberFormatter
{
    /// <summary>
    /// Upper-case hex, zero padded to ceil(width / 4) digits.
    /// When <paramref name="group"/> is set, a space is inserted every 2 digits (one byte) counted from the LSB.
    /// </summary>
    public static string ToHex(uint value, int width, bool group = false)
    {
        var digits = Math.Max(1, (width + 3) / 4);
        var hex = (value & BitMath.Mask(width)).ToString("X" + digits, CultureInfo.InvariantCulture);
        if (!group || hex.Length <= 2) return hex;

        var sb = new StringBuilder(hex.Length + hex.Length / 2);
        for (var i = 0; i < hex.Length; i++)
        {
            if (i > 0 && (hex.Length - i) % 2 == 0)
                sb.Append(' ');
            sb.Append(hex[i]);
        }
        return sb.ToString();
    }

    public static string ToDec(uint value, int width) =>
        (value & BitMath.Mask(width)).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Binary text of exactly <paramref name="width"/> digits.
    /// When <paramref name="group"/> is set, a space is inserted every 4 bits counted from the LSB.
    /// </summary>
    public static string ToBin(uint value, int width, bool group = true)
    {
        var sb = new StringBuilder(width + width / 4);
        for (var i = width - 1; i >= 0; i--)
        {
            sb.Append(BitMath.GetBit(value, i) ? '1' : '0');
            if (group && i > 0 && i % 4 == 0)
                sb.Append(' ');
        }
        return sb.ToString();
    }

    /// <summary>Space separated bytes, e.g. "78 56 34 12" for little-endian 0x12345678.</summary>
    public static string ToBytes(uint value, int width, bool littleEndian)
    {
        var count = Math.Max(1, (width + 7) / 8);
        var bytes = new string[count];
        for (var i = 0; i < count; i++)
        {
            var b = (byte)(value >> (8 * i));
            bytes[littleEndian ? i : count - 1 - i] = b.ToString("X2", CultureInfo.InvariantCulture);
        }
        return string.Join(' ', bytes);
    }
}
