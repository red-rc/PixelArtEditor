using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using HeyRed.ImageSharp.Heif.Formats.Heif;
using PixelArtEditor.Models.Canvas;
using SixLabors.ImageSharp;
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlphaFormat = PixelArtEditor.Models.Canvas.AlphaFormat;

namespace PixelArtEditor.AppServices.Image;

public static class ImageExportService
{
    private static readonly List<FilePickerFileType> ExportFileTypes =
    [
        new("PNG Image")              { Patterns = ["*.png"] },
        new("JPEG Image")             { Patterns = ["*.jpg", "*.jpeg"] },
        new("Bitmap Image")           { Patterns = ["*.bmp"] },
        new("GIF Image")              { Patterns = ["*.gif"] },
        new("TIFF Image")             { Patterns = ["*.tif", "*.tiff"] },
        new("SVG Image")              { Patterns = ["*.svg"] },
        new("WebP Image")             { Patterns = ["*.webp"] },
        new("DDS Image")              { Patterns = ["*.dds"] },
        new("AVIF Image")             { Patterns = ["*.avif"] },
        new("HEIF Image")             { Patterns = ["*.heif"] },
        new("TGA Image")              { Patterns = ["*.tga"] },
        new("Portable Bitmap")        { Patterns = ["*.pbm"] },
        new("QOI Image")              { Patterns = ["*.qoi"] },
        new("Icon")                   { Patterns = ["*.ico"] }
    ];
    public static async Task ExportImageAsync(Window dialog, PixelModel model)
    {
        var defaultType = ExportFileTypes.FirstOrDefault(t =>
            t.Patterns is not null && t.Patterns.Any(p => p.TrimStart('*', '.').Equals(model.Extension, StringComparison.OrdinalIgnoreCase)));

        var saveOptions = new FilePickerSaveOptions
        {
            Title = "Export",
            SuggestedFileName = model.Name ?? "untitled",
            DefaultExtension = model.Extension,
            FileTypeChoices = defaultType is not null
                ? [defaultType, .. ExportFileTypes.Where(t => t != defaultType)]
                : ExportFileTypes
        };

        var file = await dialog.StorageProvider.SaveFilePickerAsync(saveOptions);
        if (file == null) return;

        // беремо поточні дані пікселів з canvas (завжди RGBA32)
        var pixelData = model.Data;
        if (pixelData == null) return;

        await Task.Run(async () =>
        {
            try
            {
                // конвертуємо з RGBA32 (робочий формат) в потрібний формат для збереження
                var exportData = ConvertForExport(pixelData, model);

                // створюємо базовий Rgba32 і одразу конвертуємо в потрібний формат
                using var baseImage = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
                    exportData, model.Width, model.Height);
                using var image = ConvertToTargetFormat(baseImage, model);

                image.Metadata.HorizontalResolution = model.DpiX;
                image.Metadata.VerticalResolution = model.DpiY;

                await using var stream = await file.OpenWriteAsync();

                if (Path.GetExtension(file.Name).Equals(".svg", StringComparison.InvariantCultureIgnoreCase))
                {
                    await ExportAsSvgWrapper(image, stream, model.Width, model.Height);
                }
                else
                {
                    IImageEncoder encoder = Path.GetExtension(file.Name).ToLowerInvariant() switch
                    {
                        ".png" => BuildPngEncoder(model),
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
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export image.", ex);
            }
        });
    }

    private static byte[] ConvertForExport(byte[] bgra, PixelModel parameters)
    {
        var result = new byte[bgra.Length];

        for (var i = 0; i < result.Length; i += 4)
        {
            result[i + 0] = bgra[i + 2]; // R ← B
            result[i + 1] = bgra[i + 1]; // G ← G
            result[i + 2] = bgra[i + 0]; // B ← R
            result[i + 3] = bgra[i + 3]; // A ← A
        }

        if (parameters.Alpha == AlphaFormat.Premultiplied)
        {
            for (var i = 0; i < result.Length; i += 4)
            {
                byte a = result[i + 3];
                if (a == 0) continue;
                result[i + 0] = (byte)(result[i + 0] * a / 255);
                result[i + 1] = (byte)(result[i + 1] * a / 255);
                result[i + 2] = (byte)(result[i + 2] * a / 255);
            }
        }

        return result;
    }

    private static async Task ExportAsSvgWrapper(SixLabors.ImageSharp.Image image, Stream stream, int width, int height)
    {
        using var pngStream = new MemoryStream();
        await image.SaveAsPngAsync(pngStream);
        var base64 = System.Convert.ToBase64String(pngStream.ToArray());

        var svg = $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
          <image width="{width}" height="{height}" href="data:image/png;base64,{base64}" />
        </svg>
        """;

        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteAsync(svg);
    }

    private static SixLabors.ImageSharp.Image ConvertToTargetFormat(Image<Rgba32> baseImage, PixelModel parameters)
    {
        return (parameters.Mode, parameters.BitDepth) switch
        {
            (ColorMode.RGBA, BitDepth.Bit8) => baseImage.CloneAs<Rgba32>(),
            (ColorMode.RGB, BitDepth.Bit8) => baseImage.CloneAs<Rgb24>(),
            (ColorMode.RGBA, BitDepth.Bit16) => ToRgba64Image(baseImage),
            (ColorMode.RGB, BitDepth.Bit16) => baseImage.CloneAs<Rgb48>(),
            (ColorMode.Grayscale, BitDepth.Bit8) => baseImage.CloneAs<L8>(),
            (ColorMode.Grayscale, BitDepth.Bit16) => baseImage.CloneAs<L16>(),
            (ColorMode.RGB, BitDepth.RGB565) => baseImage.CloneAs<Bgr565>(),
            _ => baseImage.CloneAs<Rgba32>()
        };
    }

    // ToRgba64Image тепер приймає Image<Rgba32> замість byte[]
    private static Image<Rgba64> ToRgba64Image(Image<Rgba32> src)
    {
        var width = src.Width;
        var height = src.Height;
        var rgba64Data = new byte[width * height * 8];

        src.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < width; x++)
                {
                    var j = (y * width + x) * 8;
                    var r = (ushort)(row[x].R * 65535 / 255);
                    var g = (ushort)(row[x].G * 65535 / 255);
                    var b = (ushort)(row[x].B * 65535 / 255);
                    var a = (ushort)(row[x].A * 65535 / 255);

                    rgba64Data[j + 0] = (byte)(r & 0xFF);
                    rgba64Data[j + 1] = (byte)(r >> 8);
                    rgba64Data[j + 2] = (byte)(g & 0xFF);
                    rgba64Data[j + 3] = (byte)(g >> 8);
                    rgba64Data[j + 4] = (byte)(b & 0xFF);
                    rgba64Data[j + 5] = (byte)(b >> 8);
                    rgba64Data[j + 6] = (byte)(a & 0xFF);
                    rgba64Data[j + 7] = (byte)(a >> 8);
                }
            }
        });

        return SixLabors.ImageSharp.Image.LoadPixelData<Rgba64>(rgba64Data, width, height);
    }

    // PNG encoder з урахуванням bit depth
    private static PngEncoder BuildPngEncoder(PixelModel parameters)
    {
        var bitDepth = parameters.BitDepth switch
        {
            BitDepth.Bit1 => PngBitDepth.Bit1,
            BitDepth.Bit4 => PngBitDepth.Bit4,
            BitDepth.Bit8 => PngBitDepth.Bit8,
            BitDepth.Bit16 => PngBitDepth.Bit16,
            _ => PngBitDepth.Bit8
        };

        return new PngEncoder { BitDepth = bitDepth };
    }
}
