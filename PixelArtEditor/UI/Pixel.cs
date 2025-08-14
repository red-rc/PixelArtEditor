using Avalonia.Media;

namespace PixelArtEditor.UI;

public class Pixel(int x, int y, Color color)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public Color Color { get; set; } = color;
}