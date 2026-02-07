using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using HeyRed.ImageSharp.Heif.Formats.Heif;
using PixelArtEditor.ViewModels;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices;

public static class ImageExportService
{
    public static async Task ExportImageAsync(Window dialog, ExportParams LivePreviewParams)
    {
        var saveOptions = new FilePickerSaveOptions
        {
            Title = "Export",
            SuggestedFileName = "untitled",
            DefaultExtension = "png"
        };

        var file = await dialog.StorageProvider.SaveFilePickerAsync(saveOptions);
        if (file == null) return;

        var bitmap = Services.ExportPreview.GetUpdatedPreview(LivePreviewParams);
        if (bitmap == null) return;

        var (Width, Height, Alpha, Pixels) = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            return (
                bitmap.PixelSize.Width,
                bitmap.PixelSize.Height,
                bitmap.AlphaFormat ?? AlphaFormat.Unpremul,
                BitmapService.GetPixelData(bitmap)
            );
        });

        await Task.Run(async () =>
        {
            try
            {
                if (Alpha == AlphaFormat.Premul)
                {
                    for (var i = 0; i < Pixels.Length; i += 4)
                    {
                        byte a = Pixels[i + 3];
                        if (a == 0) continue;

                        Pixels[i + 0] = (byte)(Pixels[i + 0] * 255 / a);
                        Pixels[i + 1] = (byte)(Pixels[i + 1] * 255 / a);
                        Pixels[i + 2] = (byte)(Pixels[i + 2] * 255 / a);
                    }
                }

                using var image = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(Pixels, Width, Height);

                image.Metadata.HorizontalResolution = LivePreviewParams.SelectedDPI.X;
                image.Metadata.VerticalResolution = LivePreviewParams.SelectedDPI.Y;

                await using var stream = await file.OpenWriteAsync();

                IImageEncoder encoder = Path.GetExtension(file.Name).ToLowerInvariant() switch
                {
                    ".png" => new PngEncoder(),
                    ".jpg" or ".jpeg" => new JpegEncoder { Quality = 100 },
                    ".bmp" => new BmpEncoder(),
                    ".gif" => new GifEncoder(),
                    ".tif" or ".tiff" => new TiffEncoder(),
                    ".webp" => new WebpEncoder { Quality = 100 },
                    ".tga" => new TgaEncoder(),
                    ".pbm" => new PbmEncoder(),
                    ".qoi" => new QoiEncoder(),
                    ".avif" => new AvifEncoder(),
                    ".heif" => new HeifEncoder(),
                    _ => new PngEncoder()
                };

                image.Save(stream, encoder);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export image.", ex);
            }
        });
    }
}
