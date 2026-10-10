using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.AppServices.Bitmap.Quantization;

public static class MedianCut
{
    public static Palette GetPalette(Dictionary<uint, int> histogram, int colorCount, int transparentWeight = 0)
    {
        var colors = new Color[histogram.Count];
        var weights = new int[histogram.Count];
        var i = 0;

        foreach (var (key, w) in histogram)
        {
            colors[i] = QuantizationService.ToColor(key);
            weights[i++] = (int)w;
        }

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
}