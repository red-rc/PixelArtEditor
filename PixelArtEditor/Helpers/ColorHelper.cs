using Avalonia.Media;

namespace PixelArtEditor.Helpers;

public static class ColorHelper
{
    public static string AdjustBrightness(string hexColor, double factor)
    {
        var color = HexToColor(hexColor);
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

        return Color.FromArgb(color.A, r, g, b).ToString();
    }

    public static Color HexToColor(string hexColor)
    {
        var color = System.Drawing.ColorTranslator.FromHtml(hexColor);

        return Color.FromArgb(
            color.A,
            color.R,
            color.G,
            color.B
        );
    }
}
