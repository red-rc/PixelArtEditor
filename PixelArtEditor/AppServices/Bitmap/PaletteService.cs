using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.AppServices.Bitmap;

public class PaletteService
{
    public static Palette GetKMeansPalette(byte[] data, int width, byte colorCount, bool dither)
    {
        throw new NotImplementedException();
    }

    public static Palette GetMedianCutPalette(byte[] data, int width, byte colorCount, bool dither)
    {
        throw new NotImplementedException();
    }

    public static Palette GetOctreePalette(byte[] data, int width, byte colorCount, bool dither)
    {
        throw new NotImplementedException();
    }

    public static Palette GetPalette(byte[] data, int width, byte colorCount, PaletteQuantization quantizationMethod, bool dither)
    {
        return quantizationMethod switch
        {
            PaletteQuantization.KMeans => GetKMeansPalette(data, width, colorCount, dither),
            PaletteQuantization.MedianCut => GetMedianCutPalette(data, width, colorCount, dither),
            PaletteQuantization.Octree => GetOctreePalette(data, width, colorCount, dither),
            _ => GetMedianCutPalette(data, width, colorCount, dither)
        };
    }

    public static Color ResolveColorForPalette(Color color, Palette palette, BitDepth bitDepth)
    {
        var colors = palette.Colors;

        for (var i = 0; i < colors.Count; i++)
        {
            if (colors[i] == color)
                return color;
        }

        if (colors.Count < GetMaxPaletteColorIdx(bitDepth) + 1)
        {
            palette.Colors.Add(color);
            return color;
        }

        return GetClosestPaletteColor(color, palette);
    }

    public static byte? GetClosestPaletteColorIdx(Color color, Palette palette)
    {
        if (palette.Colors.Count == 0) return null;

        byte bestIdx = 0;
        var minDistance = int.MaxValue;

        for (byte i = 0; i < palette.Colors.Count; i++)
        {
            var c = palette.Colors[i];

            var dr = color.R - c.R;
            var dg = color.G - c.G;
            var db = color.B - c.B;
            var da = color.A - c.A;
            var dist = dr * dr + dg * dg + db * db + da * da;

            if (dist < minDistance)
            {
                minDistance = dist;
                bestIdx = i;

                if (dist == 0) break;
            }
        }

        return bestIdx;
    }

    public static Color GetClosestPaletteColor(Color color, Palette palette)
    {
        if (palette.Colors.Count > 0 && GetClosestPaletteColorIdx(color, palette) is byte idx)
            return palette.Colors[idx];

        return color;
    }

    public static byte GetMaxPaletteColorIdx(BitDepth bitDepth) => bitDepth switch
    {
        BitDepth.Bit1 => 1,
        BitDepth.Bit2 => 3,
        BitDepth.Bit4 => 15,
        BitDepth.Bit8 => 255,
        _ => 255
    };
}