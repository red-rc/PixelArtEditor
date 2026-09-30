using Avalonia.Media;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.Models.Canvas;

namespace PixelArtEditor.AppServices.Tools;

public static class ColorResolver
{
    public static Color Resolve(Color color, ColorMode colorMode, Palette? palette = null, BitDepth? bitDepth = null)
    {
        byte GetLuminance() => (byte)((color.R * 77 + color.G * 150 + color.B * 29) / 256);

        return colorMode switch
        {
            ColorMode.Indexed when palette is not null && bitDepth is BitDepth bd
                => PaletteService.ResolveColorForPalette(color, palette, bd),
            ColorMode.Grayscale => Color.FromArgb(255, GetLuminance(), GetLuminance(), GetLuminance()),
            ColorMode.GrayscaleAlpha => Color.FromArgb(color.A, GetLuminance(), GetLuminance(), GetLuminance()),
            ColorMode.RGB or ColorMode.BGR => Color.FromArgb(255, color.R, color.G, color.B),
            ColorMode.RG => Color.FromArgb(255, color.R, color.G, 0),
            ColorMode.A => Color.FromArgb(255, color.A, color.A, color.A),
            _ => color
        };
    }
}
