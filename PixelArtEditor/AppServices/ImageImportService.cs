using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices;

public static class ImageImportService
{
    public static async Task<WriteableBitmap?> ImportImageAsync()
    {
        var fileTypes = new List<FilePickerFileType>
        {
            new("Images")
            {
                Patterns =
                [
                    "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif",
                    "*.tif", "*.tiff", "*.webp", "*.avif", "*.heif",
                    "*.tga", "*.pbm", "*.qoi", "*.ico"
                ]
            }
        };

        var options = new FilePickerOpenOptions
        {
            Title = "Import image",
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        };

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var topLevel = desktop.Windows.FirstOrDefault(w => w.IsActive);
        if (topLevel == null) return null;

        var storageProvider = topLevel.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(options);
        IStorageFile? file = files.Count > 0 ? files[0] : null;
        if (file == null) return null;

        await using var stream = await file.OpenReadAsync();
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        if (Path.GetExtension(file.Name).Equals(".ico", StringComparison.InvariantCultureIgnoreCase))
        {
            return await Task.Run(() =>
            {
                ms.Position = 0;
                using var skBitmap = SKBitmap.Decode(ms);
                if (skBitmap == null) return null;
                return FromSkBitmap(skBitmap);
            });
        }

        return await Task.Run(() =>
        {
            ms.Position = 0;
            using var image = Image.Load<Rgba32>(ms);

            var wb = BitmapService.CreateBitmap((short)image.Width, (short)image.Height);

            using var fb = wb.Lock();
            image.ProcessPixelRows(accessor =>
            {
                unsafe
                {
                    for (var y = 0; y < image.Height; y++)
                    {
                        var srcRow = accessor.GetRowSpan(y);
                        var dst = (uint*)((byte*)fb.Address + y * fb.RowBytes);

                        fixed (Rgba32* src = srcRow)
                        {
                            for (int x = 0; x < image.Width; x++)
                            {
                                var p = src[x];
                                dst[x] =
                                    ((uint)p.A << 24) |
                                    ((uint)p.R << 16) |
                                    ((uint)p.G << 8) |
                                    p.B;
                            }
                        }
                    }
                }
            });

            return wb;
        });
    }

    private static WriteableBitmap FromSkBitmap(SKBitmap skBitmap)
    {
        var wb = BitmapService.CreateBitmap((short)skBitmap.Width, (short)skBitmap.Height, AlphaFormat.Premul);

        using var fb = wb.Lock();

        unsafe
        {
            Buffer.MemoryCopy(
                skBitmap.GetPixels().ToPointer(),
                fb.Address.ToPointer(),
                fb.RowBytes * wb.PixelSize.Height,
                skBitmap.RowBytes * skBitmap.Height);
        }

        return wb;
    }
}