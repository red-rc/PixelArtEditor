using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices.Bitmap;

public class PaletteService
{
    public static Palette GetKMeansPalette(byte[] data, byte colorCount)
    {
        throw new NotImplementedException();
    }

    public static Palette GetMedianCutPalette(byte[] data, byte colorCount)
    {
        var (colors, weights) = GetPixelColorHistogram(data);
        var boxes = new List<ColorBox> { new(colors, weights, 0, colors.Length) };

        while (boxes.Count <= colorCount) {
            var boxToSplit = GetMaxVolumeColorBox(boxes);
            if (boxToSplit is null) break;

            var channel = boxToSplit.GetChannelWithLongestRange();
            var medianLen = boxToSplit.SplitAtMedian(channel);

            var left = new ColorBox(boxToSplit.Colors, boxToSplit.Weights, boxToSplit.Start, medianLen);
            var right = new ColorBox(boxToSplit.Colors, boxToSplit.Weights, boxToSplit.Start + medianLen, 
                boxToSplit.Length - medianLen);

            boxes.Remove(boxToSplit);
            boxes.Add(left);
            boxes.Add(right);
        }

        var paletteColors = new List<Color>();

        foreach (var box in boxes)
            paletteColors.Add(box.GetAverageColor());

        return new Palette(paletteColors);
    }

    public static Palette GetOctreePalette(byte[] data, byte colorCount)
    {
        throw new NotImplementedException();
    }

    public static unsafe (Color[] colors, int[] weights) GetPixelColorHistogram(byte[] data)
    {
        var histogram = new Dictionary<Color, int>();

        fixed (byte* srcPtr = data)
        {
            uint* src = (uint*)srcPtr;
            var count = data.Length / 4;

            for (var i = 0; i < count; i++)
            {
                var b = (byte)(src[i] & 0xFF);
                var g = (byte)((src[i] >> 8) & 0xFF);
                var r = (byte)((src[i] >> 16) & 0xFF);
                var a = (byte)((src[i] >> 24) & 0xFF);

                var color = Color.FromArgb(a, r, g, b);

                if (histogram.TryGetValue(color, out var w))
                    histogram[color] = w + 1;
                else
                    histogram[color] = 1;
            }
        }

        var colors = new Color[histogram.Count];
        var weights = new int[histogram.Count];

        var idx = 0;
        foreach (var kvp in histogram)
        {
            colors[idx] = kvp.Key;
            weights[idx] = kvp.Value;
            idx++;
        }

        return (colors, weights);
    }

    private static ColorBox? GetMaxVolumeColorBox(List<ColorBox> boxes)
    {
        ColorBox? maxBox = null;
        uint maxVolume = 0;

        foreach (var box in boxes)
        {
            if (box.Length <= 1) continue;

            var volume = box.Volume;
            if (maxBox is null || volume > maxVolume)
            {
                maxVolume = volume;
                maxBox = box;
            }
        }

        return maxBox;
    }

    public static Palette GetPalette(byte[] data, byte colorCount, PaletteQuantization quantizationMethod)
    {
        return quantizationMethod switch
        {
            PaletteQuantization.KMeans => GetKMeansPalette(data, colorCount),
            PaletteQuantization.MedianCut => GetMedianCutPalette(data, colorCount),
            PaletteQuantization.Octree => GetOctreePalette(data, colorCount),
            _ => GetMedianCutPalette(data, colorCount)
        };
    }

    public static Color ResolveColorForPalette(Color color, Palette palette, BitDepth bitDepth)
    {
        for (var i = 0; i < palette.Colors.Count; i++)
        {
            if (palette.Colors[i] == color)
                return color;
        }

        if (palette.Colors.Count <= GetMaxPaletteColorIdx(bitDepth))
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

    public static byte GetMaxPaletteColorIdx(BitDepth bitDepth) => bitDepth switch
    {
        BitDepth.Bit1 => 1,
        BitDepth.Bit2 => 3,
        BitDepth.Bit4 => 15,
        BitDepth.Bit8 => 255,
        _ => 255
    };
}