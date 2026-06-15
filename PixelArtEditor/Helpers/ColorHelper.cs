using Avalonia.Media;

namespace PixelArtEditor.Helpers;

public static class ColorHelper
{
    public static Color AdjustBrightness(this Color color, double factor)
    {
        byte r, g, b;

        if (factor > 0)
        {
            r = (byte)(color.R + (255 - color.R) * factor);
            g = (byte)(color.G + (255 - color.G) * factor);
            b = (byte)(color.B + (255 - color.B) * factor);
        }
        else
        {
            double k = 1 + factor;
            r = (byte)(color.R * k);
            g = (byte)(color.G * k);
            b = (byte)(color.B * k);
        }

        return Color.FromArgb(color.A, r, g, b);
    }
}
