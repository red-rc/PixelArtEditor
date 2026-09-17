using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using HeyRed.ImageSharp.Heif.Formats.Heif;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.ImageProcessing.Formats;
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
using Image = SixLabors.ImageSharp.Image;

namespace PixelArtEditor.AppServices.ImageProcessing;

public static class ImageExportService
{
    private static readonly List<FilePickerFileType> ExportFileTypes =
    [
        new($"PNG {LocalizationService.Get("Image")}")             { Patterns = ["*.png"] },
        new($"JPEG {LocalizationService.Get("Image")}")            { Patterns = ["*.jpg", "*.jpeg"] },
        new($"Bitmap {LocalizationService.Get("Image")}")          { Patterns = ["*.bmp"] },
        new($"GIF {LocalizationService.Get("Image")}")             { Patterns = ["*.gif"] },
        new($"TIFF {LocalizationService.Get("Image")}")            { Patterns = ["*.tif", "*.tiff"] },
        new($"SVG {LocalizationService.Get("Image")}")             { Patterns = ["*.svg"] },
        new($"WebP {LocalizationService.Get("Image")}")            { Patterns = ["*.webp"] },
        new($"DDS {LocalizationService.Get("Image")}")             { Patterns = ["*.dds"] },
        new($"AVIF {LocalizationService.Get("Image")}")            { Patterns = ["*.avif"] },
        new($"HEIF {LocalizationService.Get("Image")}")            { Patterns = ["*.heif"] },
        new($"TGA {LocalizationService.Get("Image")}")             { Patterns = ["*.tga"] },
        new($"Portable {LocalizationService.Get("Image")}")        { Patterns = ["*.pbm"] },
        new($"QOI {LocalizationService.Get("Image")}")             { Patterns = ["*.qoi"] },
        new($"DICOM {LocalizationService.Get("Image")}")           { Patterns = ["*.dcm"] },
        new($"PDF {LocalizationService.Get("Document")}")          { Patterns = ["*.pdf"] },
        new($"Icon")                                               { Patterns = ["*.ico"] }
    ];
    public static async Task ExportImageAsync(Window dialog, PixelModel model)
    {
        var defaultType = ExportFileTypes.FirstOrDefault(t =>
            t.Patterns is not null && t.Patterns.Any(p => p.TrimStart('*', '.').Equals(model.Extension, StringComparison.OrdinalIgnoreCase)));

        var saveOptions = new FilePickerSaveOptions
        {
            Title = $"{LocalizationService.Get("Export")}",
            SuggestedFileName = model.Name ?? $"{LocalizationService.Get("Untitled")}",
            DefaultExtension = model.Extension,
            FileTypeChoices = defaultType is not null
                ? [defaultType, .. ExportFileTypes.Where(t => t != defaultType)]
                : ExportFileTypes
        };

        var file = await dialog.StorageProvider.SaveFilePickerAsync(saveOptions);
        if (file == null) return;

        var pixelData = model.Data;
        if (pixelData == null) return;

        await Task.Run(async () =>
        {
            try
            {
                var exportData = ConvertForExport(pixelData, model);

                using var baseImage = Image.LoadPixelData<Rgba32>(
                    exportData, model.Width, model.Height);
                using var image = ConvertToTargetFormat(baseImage, model);

                image.Metadata.HorizontalResolution = model.DpiX;
                image.Metadata.VerticalResolution = model.DpiY;

                await using var stream = await file.OpenWriteAsync();

                if (Path.GetExtension(file.Name).Equals(".dcm", StringComparison.InvariantCultureIgnoreCase))
                {
                    DicomService.Save(stream, exportData, model.Width, model.Height, model.DicomDataset);
                }
                else if (Path.GetExtension(file.Name).Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
                {
                    PdfService.Save(stream, exportData, model.Width, model.Height, model.DpiX, model.DpiY);
                }
                else if (Path.GetExtension(file.Name).Equals(".svg", StringComparison.InvariantCultureIgnoreCase))
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
                throw new InvalidOperationException($"{LocalizationService.Get("FailedExport")}", ex);
            }
        });
    }

    private static byte[] ConvertForExport(byte[] bgra, PixelModel parameters)
    {
        var result = BitmapService.SwapRB(bgra);

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

    private static async Task ExportAsSvgWrapper(Image image, Stream stream, int width, int height)
    {
        using var pngStream = new MemoryStream();
        await image.SaveAsPngAsync(pngStream);
        var base64 = Convert.ToBase64String(pngStream.ToArray());

        var svg = $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
          <image width="{width}" height="{height}" href="data:image/png;base64,{base64}" />
        </svg>
        """;

        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteAsync(svg);
    }

    private static Image ConvertToTargetFormat(Image<Rgba32> baseImage, PixelModel parameters)
    {
        return (parameters.Mode, parameters.BitDepth) switch
        {
            (ColorMode.RGBA, BitDepth.Bit8) => baseImage.CloneAs<Rgba32>(),
            (ColorMode.RGBA, BitDepth.Bit16) => baseImage.CloneAs<Rgba64>(),
            (ColorMode.RGB, BitDepth.Bit8) => baseImage.CloneAs<Rgb24>(),
            (ColorMode.RGB, BitDepth.Bit16) => baseImage.CloneAs<Rgb48>(),
            (ColorMode.RGB, BitDepth.RGB565) => baseImage.CloneAs<Bgr565>(),
            (ColorMode.Grayscale, BitDepth.Bit8) => baseImage.CloneAs<L8>(),
            (ColorMode.Grayscale, BitDepth.Bit16) => baseImage.CloneAs<L16>(),
            _ => baseImage.CloneAs<Rgba32>()
        };
    }

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