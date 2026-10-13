namespace BitsConverter.Core;

public enum NumberBase
{
    Dec,
    Hex,
    Bin,
}

/// <summary>
/// Tolerant parser for user input. Accepts 0x / 0b prefixes, '_' and whitespace separators.
/// </summary>
public static class NumberParser
{
    public static bool TryParse(string? text, NumberBase numberBase, int width, out uint value, out string error) =>
        numberBase switch
        {
            NumberBase.Hex => TryParseHex(text, width, out value, out error),
            NumberBase.Bin => TryParseBin(text, width, out value, out error),
            _ => TryParseDec(text, width, out value, out error),
        };

    public static bool TryParseDec(string? text, int width, out uint value, out string error)
    {
        value = 0;
        var s = Normalize(text);
        if (s.Length == 0) return Fail("Empty value", out error);

        ulong acc = 0;
        foreach (var c in s)
        {
            if (c is < '0' or > '9') return Fail($"Invalid decimal digit '{c}'", out error);
            acc = acc * 10 + (ulong)(c - '0');
            if (acc > BitMath.MaxValue(width)) return Fail($"Out of range (max {BitMath.MaxValue(width)})", out error);
        }
        value = (uint)acc;
        error = string.Empty;
        return true;
    }

    public static bool TryParseHex(string? text, int width, out uint value, out string error)
    {
        value = 0;
        var s = StripPrefix(Normalize(text), "0x");
        if (s.Length == 0) return Fail("Empty value", out error);

        ulong acc = 0;
        foreach (var c in s)
        {
            int digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1,
            };
            if (digit < 0) return Fail($"Invalid hex digit '{c}'", out error);
            acc = (acc << 4) | (uint)digit;
            if (acc > BitMath.MaxValue(width))
                return Fail($"Out of range (max 0x{NumberFormatter.ToHex(BitMath.MaxValue(width), width)})", out error);
        }
        value = (uint)acc;
        error = string.Empty;
        return true;
    }

    public static bool TryParseBin(string? text, int width, out uint value, out string error)
    {
        value = 0;
        var s = StripPrefix(Normalize(text), "0b");
        if (s.Length == 0) return Fail("Empty value", out error);

        ulong acc = 0;
        foreach (var c in s)
        {
            if (c is not ('0' or '1')) return Fail($"Invalid binary digit '{c}'", out error);
            acc = (acc << 1) | (uint)(c - '0');
            if (acc > BitMath.MaxValue(width)) return Fail($"Out of range (max {width} bits)", out error);
        }
        value = (uint)acc;
        error = string.Empty;
        return true;
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return string.Concat(text.Where(c => c != '_' && !char.IsWhiteSpace(c)));
    }

    private static string StripPrefix(string s, string prefix) =>
        s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? s[prefix.Length..] : s;

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }
}
