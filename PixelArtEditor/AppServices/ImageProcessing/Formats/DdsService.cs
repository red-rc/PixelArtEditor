using Pfim;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.Models.Canvas;
using System;
using System.IO;

namespace PixelArtEditor.AppServices.ImageProcessing.Formats;

public static class DdsService
{
    // Читає DDS зі стріму й повертає готовий RGBA32 byte[] + розміри
    public static (byte[] data, int width, int height)? LoadAsRgba32(Stream stream, out string? error)
    {
        error = null;
        IImage image;

        try
        {
            image = Pfimage.FromStream(stream);
        }
        catch (Exception ex)
        {
            error = $"{LocalizationService.Get("PfimFailed")}: {ex.GetType().Name}: {ex.Message}";
            return null;
        }

        using (image)
        {
            var width = image.Width;
            var height = image.Height;
            var src = image.Data;

            var dst = new byte[width * height * 4];

            switch (image.Format)
            {
                case ImageFormat.Rgba32:
                    for (int i = 0, j = 0; i < dst.Length; i += 4, j += 4)
                    {
                        dst[i + 0] = src[j + 2];
                        dst[i + 1] = src[j + 1];
                        dst[i + 2] = src[j + 0];
                        dst[i + 3] = src[j + 3];
                    }
                    break;

                case ImageFormat.Rgb24:
                    for (int i = 0, j = 0; i < dst.Length; i += 4, j += 3)
                    {
                        dst[i + 0] = src[j + 2];
                        dst[i + 1] = src[j + 1];
                        dst[i + 2] = src[j + 0];
                        dst[i + 3] = 255;
                    }
                    break;

                case ImageFormat.Rgb8:
                    for (int i = 0, j = 0; i < dst.Length; i += 4, j++)
                    {
                        dst[i + 0] = src[j];
                        dst[i + 1] = src[j];
                        dst[i + 2] = src[j];
                        dst[i + 3] = 255;
                    }
                    break;

                case ImageFormat.R5g5b5:
                    for (int i = 0, j = 0; i < dst.Length; i += 4, j += 2)
                    {
                        var packed = (ushort)(src[j] | (src[j + 1] << 8));

                        var r5 = (packed >> 10) & 0x1F;
                        var g5 = (packed >> 5) & 0x1F;
                        var b5 = packed & 0x1F;

                        dst[i + 0] = (byte)(r5 * 255 / 31);
                        dst[i + 1] = (byte)(g5 * 255 / 31);
                        dst[i + 2] = (byte)(b5 * 255 / 31);
                        dst[i + 3] = 255;
                    }
                    break;

                case ImageFormat.R5g5b5a1:
                    for (int i = 0, j = 0; i < dst.Length; i += 4, j += 2)
                    {
                        var packed = (ushort)(src[j] | (src[j + 1] << 8));

                        var r5 = (packed >> 10) & 0x1F;
                        var g5 = (packed >> 5) & 0x1F;
                        var b5 = packed & 0x1F;
                        var a1 = (packed >> 15) & 0x1;

                        dst[i + 0] = (byte)(r5 * 255 / 31);
                        dst[i + 1] = (byte)(g5 * 255 / 31);
                        dst[i + 2] = (byte)(b5 * 255 / 31);
                        dst[i + 3] = (byte)(a1 * 255);
                    }
                    break;

                case ImageFormat.R5g6b5:
                    for (int i = 0, j = 0; i < dst.Length; i += 4, j += 2)
                    {
                        var packed = (ushort)(src[j] | (src[j + 1] << 8));

                        var r5 = (packed >> 11) & 0x1F;
                        var g6 = (packed >> 5) & 0x3F;
                        var b5 = packed & 0x1F;

                        dst[i + 0] = (byte)(r5 * 255 / 31);
                        dst[i + 1] = (byte)(g6 * 255 / 63);
                        dst[i + 2] = (byte)(b5 * 255 / 31);
                        dst[i + 3] = 255;
                    }
                    break;

                default:
                    error = $"{LocalizationService.Get("UnhandledPfim")}: {image.Format} (Width={width}, Height={height}, " +
                        $"DataLen={src.Length}, Stride={image.Stride})";
                    return null;
            }

            return (dst, width, height);
        }
    }

    public static (PixelModel? model, string? error) Load(Stream stream)
    {
        var result = LoadAsRgba32(stream, out var error);
        if (error is not null) return ((PixelModel?)null, error);
        if (result is null) return ((PixelModel?)null, $"{LocalizationService.Get("InvalidDDS")}");

        var (data, width, height) = result.Value;

        return (new PixelModel
        {
            Width = width,
            Height = height,
            Mode = ColorMode.RGBA,
            BitDepth = BitDepth.Bit8,
            Alpha = AlphaFormat.Straight,
            ColorSpace = ColorSpace.sRGB,
            DpiX = 96f,
            DpiY = 96f,
            Data = data
        }, (string?)null);
    }

    public static void Save(Stream stream, byte[] rgba, int width, int height)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write(0x20534444u); // "DDS "

        // DDS_HEADER (124 bytes)
        writer.Write(124u);                     // dwSize
        writer.Write(0x1 | 0x2 | 0x4 | 0x1000); // DDSD_CAPS|HEIGHT|WIDTH|PIXELFORMAT
        writer.Write((uint)height);
        writer.Write((uint)width);
        writer.Write((uint)(width * 4));        // pitch
        writer.Write(0u);                       // depth
        writer.Write(0u);                       // mipmap count
        for (var i = 0; i < 11; i++) writer.Write(0u); // reserved1

        // DDS_PIXELFORMAT (32 bytes)
        writer.Write(32u);       // size
        writer.Write(0x41u);     // DDPF_ALPHAPIXELS | DDPF_RGB
        writer.Write(0u);        // fourCC
        writer.Write(32u);       // RGB bit count
        writer.Write(0x00FF0000u); // R mask
        writer.Write(0x0000FF00u); // G mask
        writer.Write(0x000000FFu); // B mask
        writer.Write(0xFF000000u); // A mask

        writer.Write(0x1000u);   // dwCaps = DDSCAPS_TEXTURE
        writer.Write(0u);        // dwCaps2
        writer.Write(0u);        // dwCaps3
        writer.Write(0u);        // dwCaps4
        writer.Write(0u);        // reserved2

        // Pixel data: BGRA (DDS RGB mask above matches BGRA byte order little-endian)
        var bgra = BitmapService.SwapRB(rgba); // rgba -> bgra
        writer.Write(bgra);
    }
}