using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.AppServices.Bitmap;

public class PaletteService
{
    public static Palette GetPalette(byte[] data, int colorCount, PaletteQuantization quantizationMethod)
    {
        var transparentWeight = CountTransparentPixels(data);

        var palette = quantizationMethod switch
        {
            PaletteQuantization.KMeans => GetKMeansPalette(data, colorCount),
            PaletteQuantization.MedianCut => GetMedianCutPalette(data, colorCount, transparentWeight),
            PaletteQuantization.Octree => GetOctreePalette(data, colorCount),
            _ => GetMedianCutPalette(data, colorCount, transparentWeight)
        };

        return palette;
    }

    private static unsafe int CountTransparentPixels(byte[] data)
    {
        var count = 0;
        fixed (byte* ptr = data)
        {
            uint* src = (uint*)ptr;
            var total = data.Length / 4;

            for (var i = 0; i < total; i++)
                if ((src[i] >> 24 & 0xFF) < 128) count++;
        }

        return count;
    }

    public static Palette GetKMeansPalette(byte[] data, int colorCount)
    {
        throw new NotImplementedException();
    }

    public static Palette GetMedianCutPalette(byte[] data, int colorCount, int transparentWeight = 0)
    {
        var (colors, weights) = GetPixelColorHistogram(data);

        if (colors.Length == 0)
            return new Palette(transparentWeight > 0 ? [Color.FromArgb(0, 0, 0, 0)] : []);

        var boxes = new List<ColorBox> { new(colors, weights, 0, colors.Length) };
        var rgbSlots = transparentWeight > 0 ? colorCount - 1 : colorCount;

        while (boxes.Count < rgbSlots)
        {
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

        var candidates = new List<(Color color, long weight)>();

        foreach (var box in boxes)
        {
            long boxWeight = 0;
            var span = box.Weights.AsSpan(box.Start, box.Length);
            foreach (var w in span) boxWeight += w;
            candidates.Add((box.GetAverageColor(), boxWeight));
        }

        if (transparentWeight > 0)
            candidates.Add((Color.FromArgb(0, 0, 0, 0), transparentWeight));

        candidates.Sort((a, b) => b.weight.CompareTo(a.weight));
        var result = candidates.Count > colorCount ? candidates.GetRange(0, colorCount) : candidates;

        return new Palette([.. result.Select(c => c.color)]);
    }

    public static Palette GetOctreePalette(byte[] data, int colorCount)
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
                var a = (byte)((src[i] >> 24) & 0xFF);
                if (a < 128) continue;

                var b = (byte)(src[i] & 0xFF);
                var g = (byte)((src[i] >> 8) & 0xFF);
                var r = (byte)((src[i] >> 16) & 0xFF);

                var color = Color.FromArgb(255, r, g, b);

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
