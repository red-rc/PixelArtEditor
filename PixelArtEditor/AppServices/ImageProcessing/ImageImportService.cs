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
using ImageReader = PixelArtEditor.AppServices.ImageProcessing.ImageReaderService;
using SharpImage = SixLabors.ImageSharp.Image;

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

    public static async Task<PixelModel?> GetPixelModelFromFile(IStorageFile? file, bool showError = true)
    {
        if (file is null) return null;

        try
        {
            await using var stream = await file.OpenReadAsync();
            return await GetPixelModelFromStream(stream, file.Name, showError);
        }
        catch (Exception ex)
        {
            if (showError) await ActionService.ShowError(ex.Message);
            return null;
        }
    }

    public static async Task<PixelModel?> GetPixelModelFromStream(Stream? sourceStream, string? fileName = null, bool showError = true)
    {
        if (sourceStream is null) return null;

        var ms = new MemoryStream();
        await sourceStream.CopyToAsync(ms);

        var ext = !string.IsNullOrEmpty(fileName) ? Path.GetExtension(fileName).ToLowerInvariant() : string.Empty;
        var name = Path.GetFileNameWithoutExtension(fileName);

        if (ext == ".svg")
        {
            var (svgModel, svgError) = await Task.Run(() =>
            {
                ms.Position = 0;
                return SvgService.Load(ms);
            });

            if (svgError is not null)
            {
                if (showError) await ActionService.ShowError(svgError);
                return null;
            }

            if (svgModel is not null)
            {
                svgModel.Name = name;
                svgModel.Extension = "svg";
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
                if (showError) await ActionService.ShowError(dicomError);
                return null;
            }

            if (dicomModel is not null)
            {
                dicomModel.Name = name;
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
                if (showError) await ActionService.ShowError(pdfError);
                return null;
            }

            if (pdfModel is not null)
            {
                pdfModel.Name = name;
                pdfModel.Extension = "pdf";
            }

            return pdfModel;
        }
        else if (ext == ".dds")
        {
            var (ddsModel, ddsError) = await Task.Run(() =>
            {
                ms.Position = 0;
                return DdsService.Load(ms);
            });

            if (ddsError is not null)
            {
                if (showError) await ActionService.ShowError(ddsError);
                return null;
            }

            if (ddsModel is not null)
            {
                ddsModel.Name = name;
                ddsModel.Extension = "dds";
            }

            return ddsModel;
        }

        var (model, detectedExt, error) = await Task.Run(() =>
        {
            ms.Position = 0;

            if (ext == ".ico")
            {
                var extracted = IcoService.ExtractLargestImage(ms);
                if (extracted is null) return (null, null, $"{LocalizationService.Get("InvalidICO")}");

                ms.SetLength(0);
                ms.Write(extracted, 0, extracted.Length);
            }

            ms.Position = 0;

            ImageInfo? info;
            try { info = SharpImage.Identify(ms); }
            catch (UnknownImageFormatException) { return (null, null, $"{LocalizationService.Get("UnsupportedImage")}"); }
            catch (InvalidImageContentException) { return (null, null, $"{LocalizationService.Get("FileCorrupted")}"); }

            if (info is null) return (null, null, $"{LocalizationService.Get("CantRead")}");

            string? formatExt = info.Metadata.DecodedImageFormat?.FileExtensions.FirstOrDefault();

            ms.Position = 0;
            using var image = SharpImage.Load(ms);
            image.Mutate(x => x.AutoOrient());

            var indexedMeta = ImageReader.ExtractIndexedMetadata(image, ms);
            if (indexedMeta is not null)
            {
                ms.Position = 0;
                using var indexedImage = image.CloneAs<Rgba32>();
                var result = ImageReader.ReadIndexed(indexedImage, indexedMeta.Palette, indexedMeta.BitDepth);

                return result is null
                   ? (null, null, LocalizationService.Get("InvalidIndexed"))
                   : (result, formatExt, (string?)null);
            }

            PixelModel model = image switch
            {
                Image<Rgb24> img => ImageReader.ReadRgb24(img),
                Image<Rgb48> img => ImageReader.ReadRgb48(img),
                Image<Rgba32> img => ImageReader.ReadRgba32(img),
                Image<Rgba64> img => ImageReader.ReadRgba64(img),
                Image<Rgba1010102> img => ImageReader.ReadRgba1010102(img),
                Image<Bgr24> img => ImageReader.ReadBgr24(img),
                Image<Bgr565> img => ImageReader.ReadBgr565(img),
                Image<Bgra32> img => ImageReader.ReadBgra32(img),
                Image<Bgra4444> img => ImageReader.ReadBgra4444(img),
                Image<Bgra5551> img => ImageReader.ReadBgra5551(img),
                Image<Argb32> img => ImageReader.ReadArgb32(img),
                Image<Abgr32> img => ImageReader.ReadAbgr32(img),
                Image<Rg32> img => ImageReader.ReadRg32(img),
                Image<L8> img => ImageReader.ReadL8(img),
                Image<L16> img => ImageReader.ReadL16(img),
                Image<La16> img => ImageReader.ReadLa16(img),
                Image<La32> img => ImageReader.ReadLa32(img),
                Image<A8> img => ImageReader.ReadA8(img),
                _ => ImageReader.ReadFallback(image)
            };

            return (model, formatExt, (string?)null);
        });

        if (error is not null)
        {
            if (showError) await ActionService.ShowError(error);
            return null;
        }

        if (model is not null)
        {
            model.Name = name;
            model.Extension = string.IsNullOrEmpty(ext) ? (detectedExt ?? "png") : ext.TrimStart('.');
        }

        return model;
    }
}