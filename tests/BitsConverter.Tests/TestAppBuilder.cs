using Avalonia;
using Avalonia.Headless;
using BitsConverter.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace BitsConverter.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
