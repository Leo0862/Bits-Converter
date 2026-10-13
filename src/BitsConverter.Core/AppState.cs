using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitsConverter.Core;

public sealed class FieldDto
{
    public string Name { get; set; } = string.Empty;
    public int Msb { get; set; }
    public int Lsb { get; set; }
}

/// <summary>A reusable field layout saved to / loaded from a JSON file.</summary>
public sealed class FieldPreset
{
    public int Width { get; set; } = 8;
    public List<FieldDto> Fields { get; set; } = [];
}

/// <summary>State restored on the next application start.</summary>
public sealed class AppState
{
    public int Width { get; set; } = 8;
    public uint Value { get; set; }
    public List<FieldDto> Fields { get; set; } = [];
    public bool ShowTable { get; set; }
    public int ShiftCount { get; set; } = 1;
}

public static class StateSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static List<FieldDto> ToDto(IEnumerable<BitField> fields) =>
        FieldLayout.Sort(fields).Select(f => new FieldDto { Name = f.Name, Msb = f.Msb, Lsb = f.Lsb }).ToList();

    public static IReadOnlyList<BitField> FromDto(IEnumerable<FieldDto>? fields) =>
        FieldLayout.Sort((fields ?? []).Select(f => new BitField(f.Msb, f.Lsb, f.Name?.Trim() ?? string.Empty)));

    public static string SerializePreset(FieldPreset preset) => JsonSerializer.Serialize(preset, Options);

    public static bool TryDeserializePreset(string json, out FieldPreset preset, out IReadOnlyList<BitField> fields, out string error)
    {
        preset = new FieldPreset();
        fields = [];
        try
        {
            preset = JsonSerializer.Deserialize<FieldPreset>(json, Options) ?? throw new JsonException("Empty document");
        }
        catch (JsonException ex)
        {
            error = "Invalid preset file: " + ex.Message;
            return false;
        }

        if (!BitMath.IsSupportedWidth(preset.Width))
        {
            error = $"Unsupported width {preset.Width} (expected {string.Join(", ", BitMath.SupportedWidths)})";
            return false;
        }

        var parsed = FromDto(preset.Fields);
        if (!FieldLayout.TryValidate(parsed, out error)) return false;

        fields = parsed;
        return true;
    }

    public static string SerializeState(AppState state) => JsonSerializer.Serialize(state, Options);

    /// <summary>Reads saved state; invalid parts fall back to defaults instead of failing.</summary>
    public static AppState DeserializeStateOrDefault(string? json)
    {
        AppState state;
        try
        {
            state = string.IsNullOrWhiteSpace(json) ? new AppState() : JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
        }
        catch (JsonException)
        {
            return new AppState();
        }

        if (!BitMath.IsSupportedWidth(state.Width)) state.Width = 8;
        state.Value &= BitMath.Mask(state.Width);
        if (!FieldLayout.TryValidate(FromDto(state.Fields), out _)) state.Fields = [];
        state.ShiftCount = Math.Clamp(state.ShiftCount, 1, state.Width - 1);
        return state;
    }
}
