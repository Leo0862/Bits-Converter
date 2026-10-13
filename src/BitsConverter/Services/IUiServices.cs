namespace BitsConverter.Services;

/// <summary>Platform services the view model needs but cannot create on its own.</summary>
public interface IUiServices
{
    Task SetClipboardTextAsync(string text);

    /// <summary>Asks the user for a preset file and returns its content, or null if cancelled.</summary>
    Task<string?> OpenPresetAsync();

    /// <summary>Asks the user where to save a preset; returns false if cancelled.</summary>
    Task<bool> SavePresetAsync(string content);
}
