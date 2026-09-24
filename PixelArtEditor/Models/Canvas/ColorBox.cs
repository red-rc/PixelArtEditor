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

    public int SplitAtMedian(ChannelType channel)
    {
        var medianLen = Length / 2;
        var colorSpan = Colors.AsSpan(Start, Length);
        var weightSpan = Weights.AsSpan(Start, Length);

        Func<Color, byte> key = channel switch
        {
            ChannelType.Red => c => c.R,
            ChannelType.Green => c => c.G,
            ChannelType.Blue => c => c.B,
            _ => c => c.A
        };

        QuickSelect(colorSpan, weightSpan, medianLen, key);

        return medianLen;
    }

    private static void QuickSelect(Span<Color> colors, Span<int> weights, int k, Func<Color, byte> key)
    {
        int lo = 0, hi = colors.Length - 1;
        while (lo < hi)
        {
            var pivot = key(colors[(lo + hi) / 2]);
            int i = lo, j = hi;
            while (i <= j)
            {
                while (key(colors[i]) < pivot) i++;
                while (key(colors[j]) > pivot) j--;
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
        var sumWeight = 0;

        var span = Colors.AsSpan(Start, Length);
    
        for (int i = 0; i < span.Length; i++)
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