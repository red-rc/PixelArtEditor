using PixelArtEditor.Models.Canvas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;

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
