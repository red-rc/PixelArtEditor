using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.AppServices.Image;

public static class PixelModelService
{
    public static byte[] ToRgba32(PixelModel model)
    {
        return (model.Mode, model.BitDepth) switch
        {
            (ColorMode.RGBA, BitDepth.Bit8) => model.Data, // вже готово
            (ColorMode.RGB, BitDepth.Bit8) => Rgb24ToRgba32(model),
            (ColorMode.RGBA, BitDepth.Bit16) => Rgba64ToRgba32(model),
            (ColorMode.RGB, BitDepth.Bit16) => Rgb48ToRgba32(model),
            (ColorMode.Grayscale, BitDepth.Bit8) => L8ToRgba32(model),
            (ColorMode.Grayscale, BitDepth.Bit16) => L16ToRgba32(model),
            (ColorMode.RGB, BitDepth.RGB565) => Rgb565ToRgba32(model),
            (ColorMode.Indexed, BitDepth.Bit8) => Indexed8ToRgba32(model),
            (ColorMode.Indexed, BitDepth.Bit4) => Indexed4ToRgba32(model),
            (ColorMode.Indexed, BitDepth.Bit1) => Indexed1ToRgba32(model),
            _ => throw new NotImplementedException($"{model.Mode} {model.BitDepth} ще не реалізовано")
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

    // Indexed8 → RGBA32: індекс → колір з палітри
    private static byte[] Indexed8ToRgba32(PixelModel model)
    {
        if (model.Palette is null)
            throw new InvalidOperationException("Indexed режим без палітри");

        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];

        for (int i = 0, j = 0; i < dst.Length; i += 4, j++)
        {
            var color = model.Palette.Colors[src[j]];
            dst[i + 0] = color.R;
            dst[i + 1] = color.G;
            dst[i + 2] = color.B;
            dst[i + 3] = color.A;
        }
        return dst;
    }

    // Indexed4 → RGBA32: два пікселі на байт
    private static byte[] Indexed4ToRgba32(PixelModel model)
    {
        if (model.Palette is null)
            throw new InvalidOperationException("Indexed режим без палітри");

        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];
        var dstIdx = 0;

        for (var j = 0; j < src.Length; j++)
        {
            var hi = (src[j] >> 4) & 0xF; // перший піксель
            var lo = src[j] & 0xF; // другий піксель

            var c1 = model.Palette.Colors[hi];
            dst[dstIdx++] = c1.R;
            dst[dstIdx++] = c1.G;
            dst[dstIdx++] = c1.B;
            dst[dstIdx++] = c1.A;

            var c2 = model.Palette.Colors[lo];
            dst[dstIdx++] = c2.R;
            dst[dstIdx++] = c2.G;
            dst[dstIdx++] = c2.B;
            dst[dstIdx++] = c2.A;
        }
        return dst;
    }

    // Indexed1 → RGBA32: вісім пікселів на байт
    private static byte[] Indexed1ToRgba32(PixelModel model)
    {
        if (model.Palette is null)
            throw new InvalidOperationException("Indexed режим без палітри");

        var src = model.Data;
        var dst = new byte[model.Width * model.Height * 4];
        var dstIdx = 0;

        for (var j = 0; j < src.Length; j++)
        {
            for (var bit = 7; bit >= 0; bit--) // MSB first
            {
                var idx = (src[j] >> bit) & 1;
                var color = model.Palette.Colors[idx];
                dst[dstIdx++] = color.R;
                dst[dstIdx++] = color.G;
                dst[dstIdx++] = color.B;
                dst[dstIdx++] = color.A;
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
