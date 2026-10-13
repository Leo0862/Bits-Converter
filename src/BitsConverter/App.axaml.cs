using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BitsConverter.Services;
using BitsConverter.ViewModels;
using BitsConverter.Views;

namespace BitsConverter;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var viewModel = new MainWindowViewModel(window, SettingsStore.Load());
            window.DataContext = viewModel;
            window.Closing += (_, _) => SettingsStore.Save(viewModel.ToState());
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
