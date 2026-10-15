using BitsConverter.Core;
using BitsConverter.Services;

namespace BitsConverter.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BitsConverterTests", Guid.NewGuid().ToString("N"));

    private string Primary => Path.Combine(_root, "app", "settings.json");

    private string Fallback => Path.Combine(_root, "appdata", "BitsConverter", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void DefaultPath_IsNextToExecutable() =>
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "settings.json"), SettingsStore.FilePath);

    [Fact]
    public void SaveThenLoad_UsesPrimaryLocation()
    {
        var written = SettingsStore.Save(new AppState { Width = 16, Value = 0x1234 }, Primary, Fallback);

        Assert.Equal(Primary, written);
        Assert.False(File.Exists(Fallback));
        var state = SettingsStore.Load(Primary, Fallback);
        Assert.Equal(16, state.Width);
        Assert.Equal(0x1234u, state.Value);
    }

    [Fact]
    public void Load_FallsBackToOldLocation()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Fallback)!);
        File.WriteAllText(Fallback, StateSerializer.SerializeState(new AppState { Width = 24 }));

        Assert.Equal(24, SettingsStore.Load(Primary, Fallback).Width);
    }

    [Fact]
    public void Load_PrefersPrimaryOverFallback()
    {
        SettingsStore.Save(new AppState { Width = 32 }, Primary, Fallback);
        Directory.CreateDirectory(Path.GetDirectoryName(Fallback)!);
        File.WriteAllText(Fallback, StateSerializer.SerializeState(new AppState { Width = 24 }));

        Assert.Equal(32, SettingsStore.Load(Primary, Fallback).Width);
    }

    [Fact]
    public void Save_FallsBackWhenPrimaryIsNotWritable()
    {
        // A file where the directory should be makes the primary location unwritable on every OS.
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "app"), "not a directory");

        var written = SettingsStore.Save(new AppState { Width = 16 }, Primary, Fallback);

        Assert.Equal(Fallback, written);
        Assert.Equal(16, SettingsStore.Load(Primary, Fallback).Width);
    }

    [Fact]
    public void Load_WithoutFilesReturnsDefaults() =>
        Assert.Equal(8, SettingsStore.Load(Primary, Fallback).Width);
}
