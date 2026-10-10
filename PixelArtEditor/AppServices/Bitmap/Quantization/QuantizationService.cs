using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PixelArtEditor.AppServices.Bitmap.Quantization;

public class QuantizationService
{
    public static Color ToColor(uint p) => Color.FromArgb((byte)(p >> 24), (byte)(p >> 16), (byte)(p >> 8), (byte)p);
    public static uint Pack(byte r, byte g, byte b, byte a = 255) => b | ((uint)g << 8) | ((uint)r << 16) | ((uint)a << 24);

    public static unsafe Dictionary<uint, int> GetHistogram(byte[] data)
    {
        var histogram = new Dictionary<uint, int>();

        fixed (byte* srcPtr = data)
        {
            uint* src = (uint*)srcPtr;
            var count = data.Length / 4;

            for (var i = 0; i < count; i++)
            {
                var p = src[i];
                if ((p >> 24) < 128) continue;
                ref var weight = ref CollectionsMarshal.GetValueRefOrAddDefault(histogram, p | 0xFF000000u, out _);
                weight++;
            }
        }

        return histogram;
    }

    public static Palette BuildPalette(Dictionary<uint, int> candidates, int transparentWeight, int colorCount)
    {
        if (transparentWeight > 0)
            candidates[0] = transparentWeight;

        var arr = candidates.ToArray();
        Array.Sort(arr, (a, b) => b.Value.CompareTo(a.Value));

        var result = new List<Color>(Math.Min(colorCount, arr.Length));
        for (var i = 0; i < arr.Length && i < colorCount; i++)
            result.Add(ToColor(arr[i].Key));

        return new Palette(result);
    }
}