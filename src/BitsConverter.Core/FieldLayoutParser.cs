using System.Globalization;
using System.Text.RegularExpressions;

namespace BitsConverter.Core;

/// <summary>
/// Converts between field layout text and <see cref="BitField"/> lists.
/// Accepted items, separated by ',', ';' or new lines:
/// <c>15:10</c>, <c>[15:10]</c>, <c>7</c>, <c>NAME[15:10]</c>, <c>NAME=15:10</c>.
/// </summary>
public static partial class FieldLayoutParser
{
    private static readonly char[] Separators = [',', ';', '\n', '\r'];

    [GeneratedRegex(@"^(?<name>[^\[\]=:]*?)\s*=?\s*\[\s*(?<msb>\d+)\s*(?::\s*(?<lsb>\d+)\s*)?\]$")]
    private static partial Regex BracketForm();

    [GeneratedRegex(@"^(?:(?<name>[^\[\]=:]*?)\s*=\s*)?(?<msb>\d+)\s*(?::\s*(?<lsb>\d+))?$")]
    private static partial Regex PlainForm();

    public static bool TryParse(string? text, out IReadOnlyList<BitField> fields, out string error)
    {
        fields = [];
        var result = new List<BitField>();
        var items = (text ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var item in items)
        {
            var match = BracketForm().Match(item);
            if (!match.Success) match = PlainForm().Match(item);
            if (!match.Success)
            {
                error = $"Cannot parse '{item}'";
                return false;
            }

            if (!int.TryParse(match.Groups["msb"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var msb) ||
                !int.TryParse(match.Groups["lsb"].Success ? match.Groups["lsb"].Value : match.Groups["msb"].Value,
                    NumberStyles.None, CultureInfo.InvariantCulture, out var lsb))
            {
                error = $"Invalid bit index in '{item}'";
                return false;
            }
            if (msb < lsb) (msb, lsb) = (lsb, msb);

            result.Add(new BitField(msb, lsb, match.Groups["name"].Value.Trim()));
        }

        if (!FieldLayout.TryValidate(result, out error)) return false;

        fields = FieldLayout.Sort(result);
        return true;
    }

    public static string Format(IEnumerable<BitField> fields) =>
        string.Join(", ", FieldLayout.Sort(fields).Select(f => f.ToString()));
}
