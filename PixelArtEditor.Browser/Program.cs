using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;

namespace PixelArtEditor.Browser;

internal static class Program
{
    private static Task Main() => BuildAvaloniaApp()
        .WithInterFont()
        .UseReactiveUI()
        .StartBrowserAppAsync("out");

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}