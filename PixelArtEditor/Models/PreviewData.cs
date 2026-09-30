using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PixelArtEditor.Models;

public sealed class PreviewData(int width, int height, WriteableBitmap? bitmap, Color? color) : ReactiveObject
{
    private int _width = width;
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private int _height = height;
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    private WriteableBitmap? _bitmap = bitmap;
    public WriteableBitmap? Bitmap
    {
        get => _bitmap;
        set => this.RaiseAndSetIfChanged(ref _bitmap, value);
    }

    private Color? _color = color;
    public Color? Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }
}
