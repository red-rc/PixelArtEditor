using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.AppServices.ImageProcessing;

public static class PixelModelService
{
    public static byte[] ToRgba32(PixelModel model)
    {
        return (model.Mode, model.BitDepth) switch
        {
            (ColorMode.RGBA, BitDepth.Bit8) => model.Data,
            (ColorMode.RGBA, BitDepth.Bit16) => Rgba64ToRgba32(model),
            (ColorMode.RGB, BitDepth.Bit8) => Rgb24ToRgba32(model),
            (ColorMode.RGB, BitDepth.Bit16) => Rgb48ToRgba32(model),
            (ColorMode.RGB, BitDepth.RGB565) => Rgb565ToRgba32(model),
            (ColorMode.Grayscale, BitDepth.Bit8) => L8ToRgba32(model),
            (ColorMode.Grayscale, BitDepth.Bit16) => L16ToRgba32(model),
            (ColorMode.Indexed, BitDepth.Bit1) => Indexed1ToRgba32(model),
            (ColorMode.Indexed, BitDepth.Bit2) => Indexed2ToRgba32(model),
            (ColorMode.Indexed, BitDepth.Bit4) => Indexed4ToRgba32(model),
            (ColorMode.Indexed, BitDepth.Bit8) => Indexed8ToRgba32(model),
            _ => throw new NotImplementedException($"{model.Mode} {model.BitDepth} {LocalizationService.Get("NotImplemented")}")
        };
    }

    // RGB24 → RGBA32: просто додаємо A=255
    private static byte[] Rgb24ToRgba32(PixelModel model)
    {
        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];

        for (int i = 0, j = 0; i < dst.Length; i += 4, j += 3)
        {
            dst[i + 0] = src[j + 0]; // R
            dst[i + 1] = src[j + 1]; // G
            dst[i + 2] = src[j + 2]; // B
            dst[i + 3] = 255;         // A
        }

        return dst;
    }
    
    // RGB48 → RGBA32
    private static byte[] Rgb48ToRgba32(PixelModel model)
    {
        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];

        for (int i = 0, j = 0; i < dst.Length; i += 4, j += 6)
        {
            dst[i + 0] = (byte)((src[j + 0] | (src[j + 1] << 8)) * 255 / 65535); // R
            dst[i + 1] = (byte)((src[j + 2] | (src[j + 3] << 8)) * 255 / 65535); // G
            dst[i + 2] = (byte)((src[j + 4] | (src[j + 5] << 8)) * 255 / 65535); // B
            dst[i + 3] = 255;
        }

        return dst;
    }

    // RGBA64 → RGBA32: 16-bit на канал → 8-bit (беремо старший байт)
    private static byte[] Rgba64ToRgba32(PixelModel model)
    {
        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];

        for (int i = 0, j = 0; i < dst.Length; i += 4, j += 8)
        {
            // little-endian: [lo, hi] → беремо hi як 8-bit значення
            dst[i + 0] = (byte)((src[j + 0] | (src[j + 1] << 8)) * 255 / 65535); // R
            dst[i + 1] = (byte)((src[j + 2] | (src[j + 3] << 8)) * 255 / 65535); // G
            dst[i + 2] = (byte)((src[j + 4] | (src[j + 5] << 8)) * 255 / 65535); // B
            dst[i + 3] = (byte)((src[j + 6] | (src[j + 7] << 8)) * 255 / 65535); // A
        }

        return dst;
    }


    // RGB565 → RGBA32
    private static byte[] Rgb565ToRgba32(PixelModel model)
    {
        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];

        for (int i = 0, j = 0; i < dst.Length; i += 4, j += 2)
        {
            // зібрати ushort з двох байт (little-endian)
            var packed = (ushort)(src[j] | (src[j + 1] << 8));

            // розпакувати R5G6B5
            var r5 = (packed >> 11) & 0x1F;
            var g6 = (packed >> 5) & 0x3F;
            var b5 = packed & 0x1F;

            // масштабувати до 8-bit: R5→8: r * 255 / 31
            dst[i + 0] = (byte)(r5 * 255 / 31);
            dst[i + 1] = (byte)(g6 * 255 / 63);
            dst[i + 2] = (byte)(b5 * 255 / 31);
            dst[i + 3] = 255;
        }

        return dst;
    }

    // L8 → RGBA32: grayscale без альфи
    private static byte[] L8ToRgba32(PixelModel model)
    {
        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];
        var hasAlpha = model.Alpha != AlphaFormat.None;

        if (hasAlpha)
        {
            // La16: [L, A, L, A, ...]
            for (int i = 0, j = 0; i < dst.Length; i += 4, j += 2)
            {
                dst[i + 0] = src[j + 0]; // R = L
                dst[i + 1] = src[j + 0]; // G = L
                dst[i + 2] = src[j + 0]; // B = L
                dst[i + 3] = src[j + 1]; // A
            }
        }
        else
        {
            // L8: [L, L, L, ...]
            for (int i = 0, j = 0; i < dst.Length; i += 4, j++)
            {
                dst[i + 0] = src[j]; // R = L
                dst[i + 1] = src[j]; // G = L
                dst[i + 2] = src[j]; // B = L
                dst[i + 3] = 255;
            }
        }

        return dst;
    }

    // L16 → RGBA32
    private static byte[] L16ToRgba32(PixelModel model)
    {
        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];
        var hasAlpha = model.Alpha != AlphaFormat.None;

        if (hasAlpha)
        {
            // La32: [Llo,Lhi, Alo,Ahi, ...]
            for (int i = 0, j = 0; i < dst.Length; i += 4, j += 4)
            {
                var l = (byte)((src[j + 0] | (src[j + 1] << 8)) * 255 / 65535);
                dst[i + 0] = l;
                dst[i + 1] = l;
                dst[i + 2] = l;

                dst[i + 3] = (byte)((src[j + 2] | (src[j + 3] << 8)) * 255 / 65535);
            }
        }
        else
        {
            // L16: [lo, hi, lo, hi, ...]
            for (int i = 0, j = 0; i < dst.Length; i += 4, j += 2)
            {
                var l = (byte)((src[j + 0] | (src[j + 1] << 8)) * 255 / 65535);
                dst[i + 0] = l;
                dst[i + 1] = l;
                dst[i + 2] = l;
                dst[i + 3] = 255;
            }
        }

        return dst;
    }

    private static byte[] Indexed8ToRgba32(PixelModel model)
    {
        if (model.Palette is null)
            throw new InvalidOperationException($"{LocalizationService.Get("InvalidPalette")}");

        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];
        var paletteColors = model.Palette.Colors;

        for (int i = 0, j = 0; i < dst.Length && j < src.Length; i += 4, j++)
        {
            var idx = src[j];

            if (idx < paletteColors.Count)
            {
                var color = paletteColors[idx];
                dst[i + 0] = color.R;
                dst[i + 1] = color.G;
                dst[i + 2] = color.B;
                dst[i + 3] = color.A;
            }
        }

        return dst;
    }

    // Indexed4 → RGBA32: два пікселі на байт
    private static byte[] Indexed4ToRgba32(PixelModel model)
    {
        if (model.Palette is null)
            throw new InvalidOperationException($"{LocalizationService.Get("InvalidPalette")}");

        var src = model.Data;
        var totalPixels = model.Width * model.Height;
        var dst = new byte[totalPixels * 4];
        var pixelIdx = 0;
        var paletteColors = model.Palette.Colors;

        for (var j = 0; j < src.Length && pixelIdx < totalPixels; j++)
        {
            var hi = (src[j] >> 4) & 0xF;
            if (hi < paletteColors.Count)
            {
                var c1 = paletteColors[hi];
                dst[pixelIdx * 4 + 0] = c1.R;
                dst[pixelIdx * 4 + 1] = c1.G;
                dst[pixelIdx * 4 + 2] = c1.B;
                dst[pixelIdx * 4 + 3] = c1.A;
            }
            pixelIdx++;
            if (pixelIdx >= totalPixels) break;

            var lo = src[j] & 0xF;
            if (lo < paletteColors.Count)
            {
                var c2 = paletteColors[lo];
                dst[pixelIdx * 4 + 0] = c2.R;
                dst[pixelIdx * 4 + 1] = c2.G;
                dst[pixelIdx * 4 + 2] = c2.B;
                dst[pixelIdx * 4 + 3] = c2.A;
            }
            pixelIdx++;
        }

        return dst;
    }

    // Indexed2 → RGBA32: чотири пікселі на байт
    private static byte[] Indexed2ToRgba32(PixelModel model)
    {
        if (model.Palette is null)
            throw new InvalidOperationException($"{LocalizationService.Get("InvalidPalette")}");

        var src = model.Data;
        var totalPixels = model.Width * model.Height;
        var dst = new byte[totalPixels * 4];
        var pixelIdx = 0;
        var paletteColors = model.Palette.Colors;

        for (var j = 0; j < src.Length && pixelIdx < totalPixels; j++)
        {
            for (var shift = 6; shift >= 0; shift -= 2)
            {
                if (pixelIdx >= totalPixels) break;
                var idx = (src[j] >> shift) & 0x03;
                if (idx < paletteColors.Count)
                {
                    var color = paletteColors[idx];
                    dst[pixelIdx * 4 + 0] = color.R;
                    dst[pixelIdx * 4 + 1] = color.G;
                    dst[pixelIdx * 4 + 2] = color.B;
                    dst[pixelIdx * 4 + 3] = color.A;
                }
                pixelIdx++;
            }
        }

        return dst;
    }

    // Indexed1 → RGBA32: вісім пікселів на байт
    private static byte[] Indexed1ToRgba32(PixelModel model)
    {
        if (model.Palette is null)
            throw new InvalidOperationException($"{LocalizationService.Get("InvalidPalette")}");

        var src = model.Data;
        var totalPixels = model.Width * model.Height;
        var dst = new byte[totalPixels * 4];
        var pixelIdx = 0;
        var paletteColors = model.Palette.Colors;

        for (var j = 0; j < src.Length && pixelIdx < totalPixels; j++)
        {
            for (var bit = 7; bit >= 0; bit--)
            {
                if (pixelIdx >= totalPixels) break;
                var idx = (src[j] >> bit) & 1;
                if (idx < paletteColors.Count)
                {
                    var color = paletteColors[idx];
                    dst[pixelIdx * 4 + 0] = color.R;
                    dst[pixelIdx * 4 + 1] = color.G;
                    dst[pixelIdx * 4 + 2] = color.B;
                    dst[pixelIdx * 4 + 3] = color.A;
                }
                pixelIdx++;
            }
        }

        return dst;
    }

    public static byte[] CreateRgba32(int width, int height, Color color)
    {
        var data = new byte[width * height * 4];
        for (var i = 0; i < data.Length; i += 4)
        {
            data[i + 0] = color.R;
            data[i + 1] = color.G;
            data[i + 2] = color.B;
            data[i + 3] = color.A;
        }

        return data;
    }
}