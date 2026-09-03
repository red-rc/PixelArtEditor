using Avalonia;

namespace PixelArtEditor.Helpers;

public static class PreviewHelper
{
    public static Rect GetAspectRect(double width, int dataWidth, int dataHeight)
    {
        var ratio = (double)dataWidth / dataHeight;

        return width / ratio > width
            ? new Rect((width - width * ratio) / 2, 0, width * ratio, width)
            : new Rect(0, (width - width / ratio) / 2, width, width / ratio);
    }
}
