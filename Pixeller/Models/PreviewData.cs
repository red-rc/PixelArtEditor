using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI.SourceGenerators;

namespace Pixeller.Models;

public sealed partial class PreviewData(int width, int height, WriteableBitmap? bitmap, Color? color) : ReactiveObject
{
    [Reactive] public partial int Width { get; set; } = width;
    [Reactive] public partial int Height { get; set; } = height;
    [Reactive] public partial WriteableBitmap? Bitmap { get; set; } = bitmap;
    [Reactive] public partial Color? Color { get; set; } = color;
}
