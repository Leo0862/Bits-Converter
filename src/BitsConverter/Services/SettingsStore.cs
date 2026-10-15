using BitsConverter.Core;

namespace BitsConverter.Services;

/// <summary>
/// Loads and saves <see cref="AppState"/> in settings.json next to the executable (portable mode).
/// The per-user application data folder is used only when the executable's folder is not writable,
/// and as a source for settings saved by earlier versions.
/// </summary>
public static class SettingsStore
{
    private const string FileName = "settings.json";

    /// <summary>settings.json in the executable's folder; also correct for single-file publishes.</summary>
    public static string FilePath { get; } = Path.Combine(AppContext.BaseDirectory, FileName);

    public static string FallbackFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BitsConverter",
        FileName);

    public static AppState Load() => Load(FilePath, FallbackFilePath);

    public static void Save(AppState state) => Save(state, FilePath, FallbackFilePath);

    public static AppState Load(string filePath, string fallbackFilePath)
    {
        var json = TryRead(filePath) ?? TryRead(fallbackFilePath);
        return StateSerializer.DeserializeStateOrDefault(json);
    }

    /// <summary>Returns the path actually written, or null if neither location was writable.</summary>
    public static string? Save(AppState state, string filePath, string fallbackFilePath)
    {
        var json = StateSerializer.SerializeState(state);
        if (TryWrite(filePath, json)) return filePath;
        // Settings are a convenience; failing to save must not block closing the app.
        return TryWrite(fallbackFilePath, json) ? fallbackFilePath : null;
    }

    private static string? TryRead(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool TryWrite(string path, string content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
