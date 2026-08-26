using System;
using Avalonia;
using ReactiveUI.Avalonia;

namespace PixelArtEditor.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [
                    Win32RenderingMode.AngleEgl,
                    Win32RenderingMode.Wgl,
                    Win32RenderingMode.Software]
            })
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 1024 * 1024 * 1024 // Виділяємо 1 ГБ під GPU-кеш текстур
            })
            .WithInterFont()
            .UseReactiveUI(_ => { })
            .LogToTrace();
}
