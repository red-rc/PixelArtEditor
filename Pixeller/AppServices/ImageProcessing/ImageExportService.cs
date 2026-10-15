using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using HeyRed.ImageSharp.Heif.Formats.Heif;
using Pixeller.AppServices.Bitmap;
using Pixeller.AppServices.ImageProcessing.Formats;
using Pixeller.AppServices.ImageProcessing.Icc;
using Pixeller.Models.Canvas;
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
using SixLabors.ImageSharp.Formats.Tiff.Constants;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace Pixeller.AppServices.ImageProcessing;

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
        new($"DICOM {LocalizationService.Get("Image")}")           { Patterns = ["*.dcm", "*.dicom"] },
        new($"PDF {LocalizationService.Get("Document")}")          { Patterns = ["*.pdf"] },
        new($"Icon")                                               { Patterns = ["*.ico"] }
    ];

    public static async Task<IStorageFile?> PickExportFileAsync(Window dialog, PixelModel model)
    {
        List<FilePickerFileType>? validFileTypes;

        if (model.ColorMode is ColorMode.Indexed)
        {
            validFileTypes = [.. ExportFileTypes.Where(t => t.Patterns is not null && t.Patterns.Any(p =>
                p.Equals("*.png", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("*.bmp", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("*.gif", StringComparison.OrdinalIgnoreCase)))];
        }
        else if (model.ColorMode is ColorMode.LA)
        {
            validFileTypes = [.. ExportFileTypes.Where(t => t.Patterns is not null && t.Patterns.Any(p =>
                !p.Equals("*.jpg", StringComparison.OrdinalIgnoreCase) &&
                !p.Equals("*.jpeg", StringComparison.OrdinalIgnoreCase) &&
                !p.Equals("*.dcm", StringComparison.OrdinalIgnoreCase) &&
                !p.Equals("*.dicom", StringComparison.OrdinalIgnoreCase) &&
                !p.Equals("*.pbm", StringComparison.OrdinalIgnoreCase)))];
        }
        else if (model.ColorMode is ColorMode.CMYK)
        {
            validFileTypes = [.. ExportFileTypes.Where(t => t.Patterns is not null && t.Patterns.Any(p =>
                p.Equals("*.jpg", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("*.jpeg", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("*.pdf", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("*.tif", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("*.tiff", StringComparison.OrdinalIgnoreCase)))];
        }
        else
            validFileTypes = ExportFileTypes;

        var defaultType = validFileTypes.FirstOrDefault(t =>
            t.Patterns is not null && t.Patterns.Any(p => p.TrimStart('*', '.').Equals(model.Extension, StringComparison.OrdinalIgnoreCase)));

        var saveOptions = new FilePickerSaveOptions
        {
            Title = $"{LocalizationService.Get("Export")}",
            SuggestedFileName = model.Name ?? $"{LocalizationService.Get("Untitled")}",
            DefaultExtension = model.Extension,
            FileTypeChoices = defaultType is not null
                ? [defaultType, .. validFileTypes.Where(t => t != defaultType)]
                : validFileTypes
        };

        return await dialog.StorageProvider.SaveFilePickerAsync(saveOptions);
    }

    public static async Task ExportImageAsync(IStorageFile file, PixelModel model)
    {        
        if (model.Data is null) return;
        
        await Task.Run(async () =>
        {
            try
            {
                Palette? exportPalette = null;
                byte[] bgraData;

                if (model.ColorMode == ColorMode.Indexed && model.Palette is not null)
                {
                    var colors = ColorSpaceConverter.ConvertPalette(model.Palette.Colors, model.SrcColorSpace, model.ColorSpace);
                    bgraData = ConvertIndexedForExport(model.Data, model.Palette, colors);
                    exportPalette = new Palette(colors, model.Palette.QuantizationMethod, model.Palette.Dither);
                }
                else
                    bgraData = ColorSpaceConverter.ConvertData(model.Data, model.SrcColorSpace, model.ColorSpace);

                var exportData = ConvertDataForExport(bgraData, model);

                using var baseImage = Image.LoadPixelData<Rgba32>(exportData, model.Width, model.Height);
                using var image = ConvertToTargetFormat(baseImage, model);
        
                image.Metadata.HorizontalResolution = model.DpiX;
                image.Metadata.VerticalResolution = model.DpiY;

                var isCmyk = model.ColorMode == ColorMode.CMYK;
                var icc = isCmyk
                    ? (model.IccProfile is { } cmykIcc && IccCmykConverter.TryCreate(cmykIcc) is { CanExport: true } ? cmykIcc : null)
                    : model.ColorSpace == ColorSpace.Custom ? model.IccProfile : IccProfiles.Load(model.ColorSpace);

                if (icc is not null) image.Metadata.IccProfile = new IccProfile(icc);

                await using var stream = await file.OpenWriteAsync();
        
                var extension = Path.GetExtension(file.Name).ToLowerInvariant();

                if (isCmyk && extension is ".tif" or ".tiff")
                    TiffCmykService.Save(stream, bgraData, model.Width, model.Height, model.DpiX, model.DpiY, model.BitDepth, model.Compression, icc);
                else if (isCmyk && extension == ".pdf")
                    PdfService.SaveCmyk(stream, bgraData, model.Width, model.Height, model.DpiX, model.DpiY, icc);
                else if (isCmyk && extension is ".jpg" or ".jpeg" && icc is not null)
                    JpegCmykCodec.Encode(stream, CmykConverter.FromBgra(bgraData, icc), model.Width, model.Height, model.Quality, icc);
                else if (extension is ".dcm" or ".dicom")
                    DicomService.Save(stream, exportData, model.Width, model.Height, model.DicomDataset);
                else if (extension is ".pdf")
                    PdfService.Save(stream, exportData, model.Width, model.Height, model.DpiX, model.DpiY);
                else if (extension is ".ico")
                    IcoService.Save(stream, image.CloneAs<Rgba32>());
                else if (extension is ".dds")
                    DdsService.Save(stream, exportData, model.Width, model.Height);
                else if (extension is ".svg")
                    await ExportAsSvgWrapper(image, stream, model.Width, model.Height);
                else
                {
                    IImageEncoder encoder = Path.GetExtension(file.Name).ToLowerInvariant() switch
                    {
                        ".png" => BuildPngEncoder(model),
                        ".jpg" or ".jpeg" => new JpegEncoder
                        {
                            Quality = model.Quality,
                            ColorType = isCmyk ? JpegEncodingColor.Cmyk : null
                        },
                        ".bmp" => BuildBmpEncoder(model),
                        ".gif" => BuildGifEncoder(model),
                        ".tif" or ".tiff" => new TiffEncoder { Compression = BuildTiffCompression(model.Compression) },
                        ".webp" => new WebpEncoder { Quality = model.Quality },
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

    private static byte[] ConvertIndexedForExport(byte[] bgra, Palette src, List<Avalonia.Media.Color> dst)
    {
        var map = new Dictionary<uint, uint>(src.Colors.Count);
        for (var i = 0; i < src.Colors.Count; i++)
        {
            var s = src.Colors[i]; var d = dst[i];
            map[Pack(s)] = Pack(d);
        }

        var res = new byte[bgra.Length];
        var p = MemoryMarshal.Cast<byte, uint>(bgra.AsSpan());
        var o = MemoryMarshal.Cast<byte, uint>(res.AsSpan());

        for (var i = 0; i < p.Length; i++)
            o[i] = map.TryGetValue(p[i], out var v) ? v : p[i];

        return res;
    }

    private static uint Pack(Avalonia.Media.Color c) => c.B | ((uint)c.G << 8) | ((uint)c.R << 16) | ((uint)c.A << 24);

    private static unsafe byte[] ConvertDataForExport(byte[] bgra, PixelModel model)
    {
        var result = BitmapService.SwapRB(bgra);

        if (model.AlphaFormat == AlphaFormat.Premultiplied)
        {
            fixed (byte* ptr = result)
            {
                for (var i = 0; i < result.Length; i += 4)
                {
                    byte* pixel = ptr + i;
        
                    byte a = pixel[3];
                    if (a == 0) continue;
        
                    pixel[0] = (byte)(pixel[0] * a / 255);
                    pixel[1] = (byte)(pixel[1] * a / 255);
                    pixel[2] = (byte)(pixel[2] * a / 255);
                }
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

    private static Image ConvertToTargetFormat(Image<Rgba32> baseImage, PixelModel model)
    {
        return (model.ColorMode, model.BitDepth) switch
        {
            (ColorMode.RGB, BitDepth.Bit8) => baseImage.CloneAs<Rgb24>(),
            (ColorMode.RGB, BitDepth.Bit16) => baseImage.CloneAs<Rgb48>(),
            (ColorMode.RGBA, BitDepth.Bit8) => baseImage.CloneAs<Rgba32>(),
            (ColorMode.RGBA, BitDepth.Bit16) => baseImage.CloneAs<Rgba64>(),
            (ColorMode.RGBA, BitDepth.Bit1010102) => baseImage.CloneAs<Rgba1010102>(),
            (ColorMode.BGR, BitDepth.Bit8) => baseImage.CloneAs<Bgr24>(),
            (ColorMode.BGR, BitDepth.Bit565) => baseImage.CloneAs<Bgr565>(),
            (ColorMode.BGRA, BitDepth.Bit8) => baseImage.CloneAs<Bgra32>(),
            (ColorMode.BGRA, BitDepth.Bit4) => baseImage.CloneAs<Bgra4444>(),
            (ColorMode.BGRA, BitDepth.Bit5551) => baseImage.CloneAs<Bgra5551>(),
            (ColorMode.ARGB, BitDepth.Bit8) => baseImage.CloneAs<Argb32>(),
            (ColorMode.ABGR, BitDepth.Bit8) => baseImage.CloneAs<Abgr32>(),
            (ColorMode.RG, BitDepth.Bit16) => baseImage.CloneAs<Rg32>(),
            (ColorMode.L, BitDepth.Bit8) => baseImage.CloneAs<L8>(),
            (ColorMode.L, BitDepth.Bit16) => baseImage.CloneAs<L16>(),
            (ColorMode.LA, BitDepth.Bit8) => baseImage.CloneAs<La16>(),
            (ColorMode.LA, BitDepth.Bit16) => baseImage.CloneAs<La32>(),
            (ColorMode.A, BitDepth.Bit8) => baseImage.CloneAs<A8>(),
            _ => baseImage.CloneAs<Rgba32>()
        };
    }

    private static TiffCompression BuildTiffCompression(TiffCompressionMode mode) => mode switch
    {
        TiffCompressionMode.None => TiffCompression.None,
        TiffCompressionMode.Deflate => TiffCompression.Deflate,
        TiffCompressionMode.PackBits => TiffCompression.PackBits,
        _ => TiffCompression.Lzw
    };

    private static PngEncoder BuildPngEncoder(PixelModel model)
    {
        var bitDepth = model.BitDepth switch
        {
            BitDepth.Bit1 => PngBitDepth.Bit1,
            BitDepth.Bit2 => PngBitDepth.Bit2,
            BitDepth.Bit4 => PngBitDepth.Bit4,
            BitDepth.Bit8 => PngBitDepth.Bit8,
            BitDepth.Bit16 => PngBitDepth.Bit16,
            BitDepth.Bit565 => PngBitDepth.Bit16,
            BitDepth.Bit5551 => PngBitDepth.Bit16,
            _ => throw new Exception($"{LocalizationService.Get("EncoderError")}")
        };

        if (model.ColorMode == ColorMode.Indexed && model.Palette is not null)
        {
            var sharpColors = model.Palette.Colors
                .Select(c => new Color(new Rgba32(c.R, c.G, c.B, c.A)))
                .ToArray();

            return new PngEncoder
            {
                BitDepth = bitDepth,
                ColorType = PngColorType.Palette,
                FilterMethod = model.PngFilter,
                CompressionLevel = model.PngCompression,
                Quantizer = new PaletteQuantizer(sharpColors)
            };
        }

        return new PngEncoder
        {
            BitDepth = bitDepth,
            FilterMethod = model.PngFilter,
            CompressionLevel = model.PngCompression
        };
    }

    private static BmpEncoder BuildBmpEncoder(PixelModel model)
    {
        var bitsPerPixel = model.BitDepth switch
        {
            BitDepth.Bit1 => BmpBitsPerPixel.Pixel1,
            BitDepth.Bit2 => BmpBitsPerPixel.Pixel2,
            BitDepth.Bit4 => BmpBitsPerPixel.Pixel4,
            BitDepth.Bit8 => BmpBitsPerPixel.Pixel8,
            BitDepth.Bit16 => BmpBitsPerPixel.Pixel16,
            BitDepth.Bit565 => BmpBitsPerPixel.Pixel16,
            BitDepth.Bit5551 => BmpBitsPerPixel.Pixel16,
            BitDepth.Bit1010102 => BmpBitsPerPixel.Pixel32,
            _ => throw new Exception($"{LocalizationService.Get("EncoderError")}")
        };

        if (model.ColorMode == ColorMode.Indexed && model.Palette is not null)
        {
            var sharpColors = model.Palette.Colors
                .Select(c => new Color(new Rgba32(c.R, c.G, c.B, c.A)))
                .ToArray();

            return new BmpEncoder
            {
                BitsPerPixel = bitsPerPixel,
                Quantizer = new PaletteQuantizer(sharpColors)
            };
        }

        return new BmpEncoder() {  BitsPerPixel = bitsPerPixel };
    }

    private static GifEncoder BuildGifEncoder(PixelModel model)
    {
        if (model.ColorMode == ColorMode.Indexed && model.Palette is not null)
        {
            var sharpColors = model.Palette.Colors
                .Select(c => new Color(new Rgba32(c.R, c.G, c.B, c.A)))
                .ToArray();

            return new GifEncoder
            {
                Quantizer = new PaletteQuantizer(sharpColors)
            };
        }

        return new GifEncoder();
    }
}