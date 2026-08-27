using Avalonia.Media;

namespace PixelArtEditor.Models;

public sealed class PreviewData(int width, int height, byte[]? pixelData, Color? color)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[]? PixelData { get; } = pixelData;
    public Color? Color { get; } = color;
}
