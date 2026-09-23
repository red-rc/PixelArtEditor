using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Color = Avalonia.Media.Color;

namespace PixelArtEditor.AppServices.ImageProcessing;

public static class ImageReaderService
{
    public static (float dpiX, float dpiY) GetDpi(ImageMetadata meta)
    {
        var x = meta.HorizontalResolution > 0 ? (float)meta.HorizontalResolution : 96f;
        var y = meta.VerticalResolution > 0 ? (float)meta.VerticalResolution : 96f;
        return (x, y);
    }

    public static PixelModel ReadRgb24(Image<Rgb24> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        var data = new byte[image.Width * image.Height * 3];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 3;
                    data[i + 0] = row[x].R;
                    data[i + 1] = row[x].G;
                    data[i + 2] = row[x].B;
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.RGB,
            BitDepth = BitDepth.Bit8,
            Alpha = AlphaFormat.None,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadRgb48(Image<Rgb48> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        var data = new byte[image.Width * image.Height * 6]; // 2 байти * 3 канали

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 6;
                    Write16(data, i + 0, row[x].R);
                    Write16(data, i + 2, row[x].G);
                    Write16(data, i + 4, row[x].B);
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.RGB,
            BitDepth = BitDepth.Bit16,
            Alpha = AlphaFormat.None,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadBgr565(Image<Bgr565> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        // 2 байти на піксель
        var data = new byte[image.Width * image.Height * 2];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 2;
                    var packed = row[x].PackedValue; // вже ushort R5G6B5
                    data[i + 0] = (byte)(packed & 0xFF);
                    data[i + 1] = (byte)(packed >> 8);
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.RGB,
            BitDepth = BitDepth.RGB565,
            Alpha = AlphaFormat.None,
            ColorSpace = ColorSpace.Linear, // RGB565 майже завжди linear (embedded/GPU)
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadRgba32(Image<Rgba32> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        var data = new byte[image.Width * image.Height * 4];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 4;
                    data[i + 0] = row[x].R;
                    data[i + 1] = row[x].G;
                    data[i + 2] = row[x].B;
                    data[i + 3] = row[x].A;
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.RGBA,
            BitDepth = BitDepth.Bit8,
            Alpha = AlphaFormat.Straight,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadRgba64(Image<Rgba64> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        // 16-bit: 2 байти на канал, 4 канали = 8 байт на піксель
        var data = new byte[image.Width * image.Height * 8];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 8;
                    // ushort → два байти, little-endian
                    Write16(data, i + 0, row[x].R);
                    Write16(data, i + 2, row[x].G);
                    Write16(data, i + 4, row[x].B);
                    Write16(data, i + 6, row[x].A);
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.RGBA,
            BitDepth = BitDepth.Bit16,
            Alpha = AlphaFormat.Straight,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadL8(Image<L8> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        var data = new byte[image.Width * image.Height];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                    data[y * image.Width + x] = row[x].PackedValue;
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.Grayscale,
            BitDepth = BitDepth.Bit8,
            Alpha = AlphaFormat.None,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadL16(Image<L16> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        var data = new byte[image.Width * image.Height * 2];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 2;
                    Write16(data, i, row[x].PackedValue);
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.Grayscale,
            BitDepth = BitDepth.Bit16,
            Alpha = AlphaFormat.None,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadLa16(Image<La16> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        // La16 = 8-bit L + 8-bit A = 2 байти на піксель
        var data = new byte[image.Width * image.Height * 2];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 2;
                    data[i + 0] = row[x].L;
                    data[i + 1] = row[x].A;
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.Grayscale,
            BitDepth = BitDepth.Bit8,
            Alpha = AlphaFormat.Straight,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel ReadLa32(Image<La32> image)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        // La32 = 16-bit L + 16-bit A = 4 байти на піксель
        var data = new byte[image.Width * image.Height * 4];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var i = (y * image.Width + x) * 4;
                    Write16(data, i + 0, row[x].L);
                    Write16(data, i + 2, row[x].A);
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.Grayscale,
            BitDepth = BitDepth.Bit16,
            Alpha = AlphaFormat.Straight,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    public static PixelModel? ReadIndexed(Image<Rgba32> image, BitDepth targetBitDepth, MemoryStream ms)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        var indices = new byte[image.Width * image.Height];

        var palette = ExtractPaletteColors(image, ms);
        if (palette.Colors.Count == 0 || palette.Colors.Count > 256) return null;

        var maxIndex = (byte)(palette.Colors.Count - 1);
        var cache = new Dictionary<Color, byte>(palette.Colors.Count);

        for (var i = 0; i < palette.Colors.Count; i++)
            cache.TryAdd(palette.Colors[i], (byte)i);

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    var pixel = row[x];
                    var pixelColor = Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);

                    if (!cache.TryGetValue(pixelColor, out var idx))
                    {
                        var bestIdx = BitmapService.GetClosestPaletteColorIdx(pixelColor, palette);
                        cache[pixelColor] = bestIdx is byte bIdx ? bIdx : (byte)0;
                    }

                    indices[y * image.Width + x] = idx <= maxIndex ? idx : maxIndex;
                }
            }
        });

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = ColorMode.Indexed,
            Palette = palette,
            BitDepth = targetBitDepth,
            Alpha = AlphaFormat.None,
            ColorSpace = ColorSpace.sRGB,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = ImageConverterService.PackIndices(indices, targetBitDepth)
        };
    }

    private static Palette ExtractPaletteColors(Image<Rgba32> image, MemoryStream ms)
    {
        var result = new List<Color>();

        var pngMeta = image.Metadata.GetPngMetadata();
        if (pngMeta?.ColorType == PngColorType.Palette && pngMeta.ColorTable.HasValue)
        {
            var colorTable = pngMeta.ColorTable.Value;

            for (var i = 0; i < colorTable.Length; i++)
            {
                var rgba = colorTable.Span[i].ToPixel<Rgba32>();
                result.Add(Color.FromArgb(rgba.A, rgba.R, rgba.G, rgba.B));
            }

            return new Palette(result);
        }

        var gifMeta = image.Metadata.GetGifMetadata();
        var gifColorTable = gifMeta?.GlobalColorTable;

        if (!gifColorTable.HasValue && image.Frames.Count > 0)
        {
            var frameMeta = image.Frames.RootFrame.Metadata.GetGifMetadata();
            gifColorTable = frameMeta?.LocalColorTable;
        }

        if (gifColorTable.HasValue)
        {
            var colorTable = gifColorTable.Value;

            for (var i = 0; i < colorTable.Length; i++)
            {
                var rgba = colorTable.Span[i].ToPixel<Rgba32>();
                result.Add(Color.FromArgb(rgba.A, rgba.R, rgba.G, rgba.B));
            }

            return new Palette(result);
        }

        var bmpPalette = BmpHelper.ExtractBmpPalette(ms);
        if (bmpPalette.Count > 0)
            return new Palette(bmpPalette);

        return new Palette([]);
    }

    public static BitDepth? DetectIndexedBitDepth(Image image)
    {
        var pngMeta = image.Metadata.GetPngMetadata();
        if (pngMeta?.ColorType == PngColorType.Palette && pngMeta.ColorTable.HasValue)
        {
            return pngMeta.ColorTable.Value.Length switch
            {
                <= 2 => BitDepth.Bit1,
                <= 4 => BitDepth.Bit2,
                <= 16 => BitDepth.Bit4,
                _ => BitDepth.Bit8
            };
        }

        var gifMeta = image.Metadata.GetGifMetadata();

        var hasGifGlobal = gifMeta?.GlobalColorTable.HasValue == true;
        var hasGifLocal = image.Frames.Count > 0 
            && image.Frames.RootFrame.Metadata.GetGifMetadata()?.LocalColorTable.HasValue == true;

        if (hasGifGlobal || hasGifLocal)
        {
            var ct = hasGifGlobal
                ? gifMeta!.GlobalColorTable!.Value
                : image.Frames.RootFrame.Metadata.GetGifMetadata().LocalColorTable!.Value;

            return ct.Length <= 16 ? BitDepth.Bit4 : BitDepth.Bit8;
        }

        var bmpMeta = image.Metadata.GetBmpMetadata();
        return bmpMeta.BitsPerPixel switch
        {
            BmpBitsPerPixel.Pixel1 => BitDepth.Bit1,
            BmpBitsPerPixel.Pixel2 => BitDepth.Bit2,
            BmpBitsPerPixel.Pixel4 => BitDepth.Bit4,
            BmpBitsPerPixel.Pixel8 => BitDepth.Bit8,
            _ => null
        };
    }

    public static PixelModel ReadFallback(Image image)
    {
        using var converted = image.CloneAs<Rgba32>();
        return ReadRgba32(converted);
    }

    // helper: записати ushort у два байти (little-endian)
    public static void Write16(byte[] data, int offset, ushort value)
    {
        data[offset + 0] = (byte)(value & 0xFF);        // молодший байт
        data[offset + 1] = (byte)((value >> 8) & 0xFF); // старший байт
    }
}
