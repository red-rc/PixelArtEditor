using Pfim;
using System;
using System.IO;

namespace PixelArtEditor.AppServices.Image.Formats;

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
}