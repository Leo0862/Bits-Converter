using BitsConverter.Core;

namespace BitsConverter.Services;

/// <summary>Loads and saves <see cref="AppState"/> in the per-user application data folder.</summary>
public static class SettingsStore
{
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BitsConverter",
        "settings.json");

    public static AppState Load()
    {
        try
        {
            return StateSerializer.DeserializeStateOrDefault(File.Exists(FilePath) ? File.ReadAllText(FilePath) : null);
        }
        catch (IOException)
        {
            return new AppState();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppState();
        }
    }

    public static void Save(AppState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, StateSerializer.SerializeState(state));
        }
        catch (IOException)
        {
            // Settings are a convenience; failing to save must not block closing the app.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
