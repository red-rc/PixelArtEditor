using PDFtoImage;
using PixelArtEditor.Models.Canvas;
using SkiaSharp;
using System;
using System.IO;

namespace PixelArtEditor.AppServices.ImageProcessing.Formats;

public static class PdfService
{
    private const int MaxPixels = 100_000_000;

    public static (PixelModel? model, string? error) Load(Stream stream, float dpi = 96f)
    {
        try
        {
            #pragma warning disable CA1416 // PDFtoImage is supported on Windows, Linux, macOS, Browser, Android, iOS
            var pageCount = Conversion.GetPageCount(stream, leaveOpen: true);
            if (pageCount <= 0)
                return (null, LocalizationService.Get("PdfNoPages"));

            stream.Position = 0;
            var renderDpi = dpi > 0 ? (int)Math.Round(dpi) : 96;
            var options = new RenderOptions { Dpi = renderDpi };

            using var rendered = Conversion.ToImage(stream, page: 0, leaveOpen: true, options: options);
            #pragma warning restore CA1416
            if (rendered is null || rendered.Width <= 0 || rendered.Height <= 0)
                return (null, LocalizationService.Get("InvalidPdf"));

            if ((long)rendered.Width * rendered.Height > MaxPixels)
                return (null, LocalizationService.Get("InvalidPdf"));

            using var converted = new SKBitmap(rendered.Width, rendered.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            using (var canvas = new SKCanvas(converted))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(rendered, 0, 0, SKSamplingOptions.Default);
            }

            var model = new PixelModel
            {
                Width = converted.Width,
                Height = converted.Height,
                Mode = ColorMode.RGBA,
                BitDepth = BitDepth.Bit8,
                Alpha = AlphaFormat.Straight,
                ColorSpace = ColorSpace.sRGB,
                DpiX = renderDpi,
                DpiY = renderDpi,
                Data = converted.Bytes
            };

            return (model, null);
        }
        catch (Exception)
        {
            return (null, LocalizationService.Get("InvalidPdf"));
        }
    }

    public static void Save(Stream stream, byte[] rgba, int width, int height, float dpiX = 96f, float dpiY = 96f)
    {
        if (width <= 0 || height <= 0 || rgba.Length != checked(width * height * 4))
            throw new InvalidOperationException(LocalizationService.Get("FailedExport"));

        var safeDpiX = dpiX > 0 ? dpiX : 96f;
        var safeDpiY = dpiY > 0 ? dpiY : 96f;
        var pageWidthPoints = width * 72f / safeDpiX;
        var pageHeightPoints = height * 72f / safeDpiY;

        using var doc = SKDocument.CreatePdf(stream);
        var page = doc.BeginPage(pageWidthPoints, pageHeightPoints);

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var skData = SKData.CreateCopy(rgba);
        using var skImage = SKImage.FromPixels(info, skData);

        page.DrawImage(skImage, new SKRect(0, 0, pageWidthPoints, pageHeightPoints), SKSamplingOptions.Default);
        doc.EndPage();
        doc.Close();
    }
}
