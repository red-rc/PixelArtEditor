using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PixelArtEditor.AppServices.ImageProcessing.Formats;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.Models.Canvas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlphaFormat = PixelArtEditor.Models.Canvas.AlphaFormat;
using SharpImage = SixLabors.ImageSharp.Image;
using ImageReader = PixelArtEditor.AppServices.ImageProcessing.ImageReaderService;

namespace PixelArtEditor.AppServices.ImageProcessing;

public static class ImageImportService
{
    private static readonly List<FilePickerFileType> ImportFileTypes =
    [
        new(LocalizationService.Get("SupportedFormats"))
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif",
                        "*.tif", "*.tiff", "*.svg", "*.dds", "*.webp",
                        "*.avif", "*.heif", "*.tga", "*.pbm", "*.qoi", "*.dcm", "*.dicom", "*.pdf", "*.ico"]
        },
        new($"PNG {LocalizationService.Get("Image")}")              { Patterns = ["*.png"] },
        new($"JPEG {LocalizationService.Get("Image")}")             { Patterns = ["*.jpg", "*.jpeg"] },
        new($"Bitmap {LocalizationService.Get("Image")}")           { Patterns = ["*.bmp"] },
        new($"GIF {LocalizationService.Get("Image")}")              { Patterns = ["*.gif"] },
        new($"TIFF {LocalizationService.Get("Image")}")             { Patterns = ["*.tif", "*.tiff"] },
        new($"SVG {LocalizationService.Get("Image")}")              { Patterns = ["*.svg"] },
        new($"WebP {LocalizationService.Get("Image")}")             { Patterns = ["*.webp"] },
        new($"DDS {LocalizationService.Get("Image")}")              { Patterns = ["*.dds"] },
        new($"AVIF {LocalizationService.Get("Image")}")             { Patterns = ["*.avif"] },
        new($"HEIF {LocalizationService.Get("Image")}")             { Patterns = ["*.heif"] },
        new($"TGA {LocalizationService.Get("Image")}")              { Patterns = ["*.tga"] },
        new($"Portable {LocalizationService.Get("Image")}")         { Patterns = ["*.pbm"] },
        new($"QOI {LocalizationService.Get("Image")}")              { Patterns = ["*.qoi"] },
        new($"DICOM {LocalizationService.Get("Image")}")            { Patterns = ["*.dcm", "*.dicom"] },
        new($"PDF {LocalizationService.Get("Document")}")           { Patterns = ["*.pdf"] },
        new($"Icon")                                                { Patterns = ["*.ico"] }
    ];
    public static async Task<PixelModel?> ImportImageAsync()
    {
        var loadOptions = new FilePickerOpenOptions
        {
            Title = $"{LocalizationService.Get("ImportImage")}",
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

            var ext = Path.GetExtension(file.Name).ToLowerInvariant();

            if (ext == ".svg")
            {
                var (svgModel, svgError) = await Task.Run(() =>
                {
                    ms.Position = 0;
                    var result = SvgService.RenderToRgba32(ms);
                    if (result is null) return ((PixelModel?)null, $"{LocalizationService.Get("InvalidSVG")}");

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
                });

                if (svgError is not null)
                {
                    await ActionService.ShowErrorAsync(svgError);
                    return null;
                }

                if (svgModel is not null)
                {
                    svgModel.Name = Path.GetFileNameWithoutExtension(file.Name);
                    svgModel.Extension = ext.TrimStart('.');
                }

                return svgModel;
            }
            else if (ext is ".dcm" or ".dicom")
            {
                var (dicomModel, dicomError) = await Task.Run(() =>
                {
                    ms.Position = 0;
                    return DicomService.Load(ms);
                });

                if (dicomError is not null)
                {
                    await ActionService.ShowErrorAsync(dicomError);
                    return null;
                }

                if (dicomModel is not null)
                {
                    dicomModel.Name = Path.GetFileNameWithoutExtension(file.Name);
                    dicomModel.Extension = "dcm";
                }

                return dicomModel;
            }
            else if (ext == ".pdf")
            {
                var (pdfModel, pdfError) = await Task.Run(() =>
                {
                    ms.Position = 0;
                    return PdfService.Load(ms);
                });

                if (pdfError is not null)
                {
                    await ActionService.ShowErrorAsync(pdfError);
                    return null;
                }

                if (pdfModel is not null)
                {
                    pdfModel.Name = Path.GetFileNameWithoutExtension(file.Name);
                    pdfModel.Extension = "pdf";
                }

                return pdfModel;
            }
            else if (ext == ".dds")
            {
                ms.Position = 0;

                var ddsResult = DdsService.LoadAsRgba32(ms, out var ddsError);

                if (ddsResult is null)
                {
                    await ActionService.ShowErrorAsync(ddsError ?? $"{LocalizationService.Get("InvalidDDS")}");
                    return null;
                }

                var (data, width, height) = ddsResult.Value;

                var ddsModel = new PixelModel
                {
                    Width = width,
                    Height = height,
                    Mode = ColorMode.RGBA,
                    BitDepth = BitDepth.Bit8,
                    Alpha = AlphaFormat.Straight,
                    ColorSpace = ColorSpace.sRGB,
                    DpiX = 96f,
                    DpiY = 96f,
                    Data = data,
                    Name = Path.GetFileNameWithoutExtension(file.Name),
                    Extension = ext.TrimStart('.')
                };

                return ddsModel;
            }

            var (model, error) = await Task.Run(() =>
            {
                ms.Position = 0;

                if (ext == ".ico")
                {
                    var extracted = IcoService.ExtractLargestImage(ms);
                    if (extracted is null) return ((PixelModel?)null, $"{LocalizationService.Get("InvalidICO")}");

                    ms.SetLength(0);
                    ms.Write(extracted, 0, extracted.Length);
                }

                ms.Position = 0;

                ImageInfo? info;
                try { info = SharpImage.Identify(ms); }
                catch (UnknownImageFormatException) { return (null, $"{LocalizationService.Get("UnsupportedImage")}"); }
                catch (InvalidImageContentException) { return (null, $"{LocalizationService.Get("FileCorrupted")}"); }

                if (info is null) return ((PixelModel?)null, $"{LocalizationService.Get("CantRead")}");

                ms.Position = 0;
                using var image = SharpImage.Load(ms);
                image.Mutate(x => x.AutoOrient());

                PixelModel result = image switch
                {
                    Image<Rgb24> img => ImageReader.ReadRgb24(img),
                    Image<Rgb48> img => ImageReader.ReadRgb48(img),
                    Image<Bgr565> img => ImageReader.ReadBgr565(img),
                    Image<Rgba32> img => ImageReader.ReadRgba32(img),
                    Image<Rgba64> img => ImageReader.ReadRgba64(img),
                    Image<L8> img => ImageReader.ReadL8(img),
                    Image<L16> img => ImageReader.ReadL16(img),
                    Image<La16> img => ImageReader.ReadLa16(img),
                    Image<La32> img => ImageReader.ReadLa32(img),
                    _ => ImageReader.ReadFallback(image)
                };

                return (result, (string?)null);
            });

            if (error is not null)
            {
                await ActionService.ShowErrorAsync(error);
                return null;
            }

            if (model is not null)
            {
                model.Name = Path.GetFileNameWithoutExtension(file.Name);
                model.Extension = Path.GetExtension(file.Name).TrimStart('.').ToLowerInvariant();
            }

            return model;
        }
    }
}