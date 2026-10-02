using Avalonia;
using Avalonia.Rendering.Composition;
using ReactiveUI.Avalonia;
using System;

namespace PixelArtEditor.Browser;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

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
           .UseReactiveUI(_ => { })
           .With(new CompositionOptions { UseSaveLayerRootClip = false })
           .LogToTrace();
}