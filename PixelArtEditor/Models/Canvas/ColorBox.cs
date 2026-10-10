using Avalonia.Media;
using System;

namespace PixelArtEditor.Models.Canvas;

public enum ChannelType { Alpha, Red, Green, Blue }

public class ColorBox
{
    public Color[] Colors;
    public int[] Weights;
    public int Start;
    public int Length;
    public uint Volume;

    public byte MinR, MaxR, MinG, MaxG, MinB, MaxB, MinA, MaxA;

    public ColorBox(Color[] colors, int[] weights, int start, int length)
    {
        Colors = colors;
        Weights = weights;
        Start = start;
        Length = length;
        ComputeBounds();
    }

    private void ComputeBounds()
    {
        byte minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0, minA = 255, maxA = 0;

        var span = Colors.AsSpan(Start, Length);
        foreach (var c in span)
        {
            if (c.R < minR) minR = c.R; if (c.R > maxR) maxR = c.R;
            if (c.G < minG) minG = c.G; if (c.G > maxG) maxG = c.G;
            if (c.B < minB) minB = c.B; if (c.B > maxB) maxB = c.B;
            if (c.A < minA) minA = c.A; if (c.A > maxA) maxA = c.A;
        }

        MinR = minR; MaxR = maxR; MinG = minG; MaxG = maxG;
        MinB = minB; MaxB = maxB; MinA = minA; MaxA = maxA;

        Volume = (uint)((maxR - minR + 1) * (maxG - minG + 1) * (maxB - minB + 1) * (maxA - minA + 1));
    }

    public ChannelType GetChannelWithLongestRange()
    {
        int r = MaxR - MinR, g = MaxG - MinG, b = MaxB - MinB, a = MaxA - MinA;
        var max = Math.Max(Math.Max(r, g), Math.Max(b, a));

        if (max == r) return ChannelType.Red;
        if (max == g) return ChannelType.Green;
        if (max == b) return ChannelType.Blue;
        return ChannelType.Alpha;
    }

    private interface IKey { byte Get(Color c); }
    private readonly struct RKey : IKey { public byte Get(Color c) => c.R; }
    private readonly struct GKey : IKey { public byte Get(Color c) => c.G; }
    private readonly struct BKey : IKey { public byte Get(Color c) => c.B; }
    private readonly struct AKey : IKey { public byte Get(Color c) => c.A; }

    public int SplitAtMedian(ChannelType channel)
    {
        var medianLen = Length / 2;
        var colorSpan = Colors.AsSpan(Start, Length);
        var weightSpan = Weights.AsSpan(Start, Length);

        switch (channel)
        {
            case ChannelType.Red: QuickSelect<RKey>(colorSpan, weightSpan, medianLen); break;
            case ChannelType.Green: QuickSelect<GKey>(colorSpan, weightSpan, medianLen); break;
            case ChannelType.Blue: QuickSelect<BKey>(colorSpan, weightSpan, medianLen); break;
            default: QuickSelect<AKey>(colorSpan, weightSpan, medianLen); break;
        }

        return medianLen;
    }

    private static void QuickSelect<TKey>(Span<Color> colors, Span<int> weights, int k) where TKey : struct, IKey
    {
        var key = default(TKey);
        int lo = 0, hi = colors.Length - 1;
        while (lo < hi)
        {
            var pivot = key.Get(colors[(lo + hi) / 2]);
            int i = lo, j = hi;
            while (i <= j)
            {
                while (key.Get(colors[i]) < pivot) i++;
                while (key.Get(colors[j]) > pivot) j--;
                if (i <= j)
                {
                    (colors[i], colors[j]) = (colors[j], colors[i]);
                    (weights[i], weights[j]) = (weights[j], weights[i]);
                    i++; j--;
                }
            }
            if (k <= j) hi = j;
            else if (k >= i) lo = i;
            else break;
        }
    }

    public Color GetAverageColor()
    {
        uint sumR = 0, sumG = 0, sumB = 0, sumA = 0;
        long sumWeight = 0;

        var span = Colors.AsSpan(Start, Length);
    
        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            var weight = Weights[Start + i];
    
            sumR += (uint)(c.R * weight);
            sumG += (uint)(c.G * weight);
            sumB += (uint)(c.B * weight);
            sumA += (uint)(c.A * weight);
    
            sumWeight += weight;
        }
    
        if (sumWeight == 0)
            return Color.FromArgb(0, 0, 0, 0);
    
        return Color.FromArgb(
            (byte)(sumA / sumWeight),
            (byte)(sumR / sumWeight),
            (byte)(sumG / sumWeight),
            (byte)(sumB / sumWeight));
    }
}