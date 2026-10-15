using Avalonia.Media;
using Pixeller.AppServices.Bitmap.Quantization;
using Pixeller.Models.Canvas;
using System.Collections.Generic;

namespace Pixeller.AppServices.Bitmap;

public class PaletteService
{
    public static Palette GetPalette(byte[] data, int colorCount, PaletteQuantization quantizationMethod, bool dither, ref Dictionary<uint, int>? histogram)
    {
        histogram ??= QuantizationService.GetHistogram(data);
        var transparentWeight = CountTransparentPixels(data);

        var palette = quantizationMethod switch
        {
            PaletteQuantization.KMeans => KMeans.GetPalette(histogram, colorCount, transparentWeight),
            PaletteQuantization.Octree => Octree.GetPalette(histogram, colorCount, transparentWeight),
            _ => MedianCut.GetPalette(histogram, colorCount, transparentWeight)
        };

        palette.Dither = dither;
        return palette;
    }

    private static unsafe int CountTransparentPixels(byte[] data)
    {
        var count = 0;
        fixed (byte* scrPtr = data)
        {
            uint* src = (uint*)scrPtr;
            var total = data.Length / 4;

            for (var i = 0; i < total; i++)
                if ((src[i] >> 24 & 0xFF) < 128) count++;
        }

        return count;
    }

    public static Color ResolveColorForPalette(Color color, Palette palette, BitDepth bitDepth)
    {
        for (var i = 0; i < palette.Colors.Count; i++)
        {
            if (palette.Colors[i] == color)
                return color;
        }

        if (palette.Colors.Count < GetPaletteMaxColorCount(bitDepth))
        {
            palette.Colors.Add(color);
            return color;
        }

        return GetClosestPaletteColor(color, palette);
    }

    public static Color GetClosestPaletteColor(Color color, Palette palette)
    {
        if (palette.Colors.Count > 0 && GetClosestPaletteColorIdx(color, palette) is byte idx)
            return palette.Colors[idx];

        return color;
    }

    public static byte? GetClosestPaletteColorIdx(Color color, Palette palette)
    {
        if (palette.Colors.Count == 0) return null;

        byte bestIdx = 0;
        var minDistance = int.MaxValue;

        for (var i = 0; i < palette.Colors.Count; i++)
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
                bestIdx = (byte)i;

                if (dist == 0) break;
            }
        }

        return bestIdx;
    }

    public static int GetPaletteMaxColorCount(BitDepth bitDepth) => bitDepth switch
    {
        BitDepth.Bit1 => 1,
        BitDepth.Bit2 => 4,
        BitDepth.Bit4 => 16,
        BitDepth.Bit8 => 256,
        _ => 256
    };
}
