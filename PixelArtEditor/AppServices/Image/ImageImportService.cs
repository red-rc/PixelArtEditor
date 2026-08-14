using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlphaFormat = PixelArtEditor.Models.Canvas.AlphaFormat;
using SharpImage = SixLabors.ImageSharp.Image;

namespace PixelArtEditor.AppServices.Image;

public static class ImageImportService
{
    private static readonly List<FilePickerFileType> ImportFileTypes =
    [
        new("All Supported Images")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif",
                        "*.tif", "*.tiff", "*.webp", "*.avif", "*.heif",
                        "*.tga", "*.pbm", "*.qoi", "*.ico"]
        },
        new("PNG Image")              { Patterns = ["*.png"] },
        new("JPEG Image")             { Patterns = ["*.jpg", "*.jpeg"] },
        new("Bitmap Image")           { Patterns = ["*.bmp"] },
        new("GIF Image")              { Patterns = ["*.gif"] },
        new("TIFF Image")             { Patterns = ["*.tif", "*.tiff"] },
        new("WebP Image")             { Patterns = ["*.webp"] },
        new("AVIF Image")             { Patterns = ["*.avif"] },
        new("HEIF Image")             { Patterns = ["*.heif"] },
        new("TGA Image")              { Patterns = ["*.tga"] },
        new("Portable Bitmap")        { Patterns = ["*.pbm"] },
        new("QOI Image")              { Patterns = ["*.qoi"] },
        new("Icon")                   { Patterns = ["*.ico"] }
    ];
    public static async Task<PixelModel?> ImportImageAsync()
    {
        var loadOptions = new FilePickerOpenOptions
        {
            Title = "Import image",
            AllowMultiple = false,
            FileTypeFilter = ImportFileTypes
        };

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var topLevel = desktop.Windows.FirstOrDefault(w => w.IsActive);
        if (topLevel is null) return null;

        var storageProvider = topLevel.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(loadOptions);
        IStorageFile? file = files.Count > 0 ? files[0] : null;

        return await GetPixelModelFromFile(file);
    }

    public static async Task<PixelModel?> GetPixelModelFromFile(IStorageFile? file)
    {
        if (file is null) return null;

        Stream stream;
        try
        {
            stream = await file.OpenReadAsync();
        }
        catch (Exception ex)
        {
            await ActionService.ShowErrorAsync(ex.Message);
            return null;
        }

        await using (stream)
        {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var (model, error) = await Task.Run(() =>
            {
                ms.Position = 0;

                ImageInfo? info;
                try
                {
                    info = SharpImage.Identify(ms);
                }
                catch (UnknownImageFormatException)
                {
                    return (null, "Unsupported image format.");
                }
                catch (InvalidImageContentException)
                {
                    return (null, "The file is corrupted or contains invalid content.");
                }

                if (info is null) return ((PixelModel?)null, "Could not read image information.");

                ms.Position = 0;
                using var image = SharpImage.Load(ms);

                PixelModel result = image switch
                {
                    Image<Rgba32> img => ReadRgba32(img),
                    Image<Rgb24> img => ReadRgb24(img),
                    Image<Rgba64> img => ReadRgba64(img),
                    Image<Rgb48> img => ReadRgb48(img),
                    Image<L8> img => ReadL8(img),
                    Image<L16> img => ReadL16(img),
                    Image<La16> img => ReadLa16(img),
                    Image<La32> img => ReadLa32(img),
                    Image<Bgr565> img => ReadBgr565(img),
                    _ => ReadFallback(image)
                };

                return (result, (string?)null);
            });

            if (error is not null)
            {
                await ActionService.ShowErrorAsync(error);
                return null;
            }

            return model;
        }
    }

    private static (float dpiX, float dpiY) GetDpi(ImageMetadata meta)
    {
        // ImageSharp зберігає DPI в метаданих
        // якщо не задано — повертаємо дефолт 96
        var x = meta.HorizontalResolution > 0 ? (float)meta.HorizontalResolution : 96f;
        var y = meta.VerticalResolution > 0 ? (float)meta.VerticalResolution : 96f;
        return (x, y);
    }

    private static PixelModel ReadRgba32(Image<Rgba32> image)
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

    private static PixelModel ReadRgb24(Image<Rgb24> image)
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

    private static PixelModel ReadRgba64(Image<Rgba64> image)
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
            BigEndian = false, // зберігаємо як little-endian
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    // helper: записати ushort у два байти (little-endian)
    private static void Write16(byte[] data, int offset, ushort value)
    {
        data[offset + 0] = (byte)(value & 0xFF);        // молодший байт
        data[offset + 1] = (byte)((value >> 8) & 0xFF); // старший байт
    }

    private static PixelModel ReadL8(Image<L8> image)
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

    private static PixelModel ReadL16(Image<L16> image)
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
            BigEndian = false,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    private static PixelModel ReadBgr565(Image<Bgr565> image)
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
            BigEndian = false,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    private static PixelModel ReadRgb48(Image<Rgb48> image)
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
            BigEndian = false,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    private static PixelModel ReadLa16(Image<La16> image)
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

    private static PixelModel ReadLa32(Image<La32> image)
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
            BigEndian = false,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = data
        };
    }

    // якщо тип взагалі невідомий — конвертуємо в RGBA32 щоб не падати
    private static PixelModel ReadFallback(SharpImage image)
    {
        using var converted = image.CloneAs<Rgba32>();
        return ReadRgba32(converted);
    }
}