using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BitsConverter.Services;
using BitsConverter.ViewModels;

namespace BitsConverter.Views;

public partial class MainWindow : Window, IUiServices
{
    private static readonly FilePickerFileType PresetFileType = new("Bit field preset")
    {
        Patterns = ["*.json"],
        MimeTypes = ["application/json"],
    };

    public MainWindow()
    {
        InitializeComponent();
        // Re-format a number box (e.g. "0x1f" -> "1F") when the user leaves it.
        AddHandler(LostFocusEvent, OnAnyLostFocus, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private static void OnAnyLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox { DataContext: NumberInputViewModel input })
            input.Normalize();
    }

    public async Task SetClipboardTextAsync(string text)
    {
        if (Clipboard is { } clipboard)
            await clipboard.SetTextAsync(text);
    }

    public async Task<string?> OpenPresetAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load bit field preset",
            AllowMultiple = false,
            FileTypeFilter = [PresetFileType, FilePickerFileTypes.All],
        });
        if (files.Count == 0) return null;

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<bool> SavePresetAsync(string content)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save bit field preset",
            SuggestedFileName = "bitfields.json",
            DefaultExtension = "json",
            FileTypeChoices = [PresetFileType],
        });
        if (file is null) return false;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        return true;
    }
}
