using PDFtoImage;
using Pixeller.Models.Canvas;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Pixeller.AppServices.ImageProcessing.Formats;

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
                ColorMode = ColorMode.RGBA,
                BitDepth = BitDepth.Bit8,
                AlphaFormat = AlphaFormat.Straight,
                ColorSpace = ColorSpace.SRgb,
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

    /// <summary>
    /// Writes a single-page PDF holding the image as a Flate-compressed DeviceCMYK (or ICCBased CMYK) XObject.
    /// </summary>
    public static void SaveCmyk(Stream stream, byte[] bgra, int width, int height, float dpiX, float dpiY, byte[]? icc)
    {
        if (width <= 0 || height <= 0 || bgra.Length != checked(width * height * 4))
            throw new InvalidOperationException(LocalizationService.Get("FailedExport"));

        var pageW = (width * 72f / (dpiX > 0 ? dpiX : 96f)).ToString("0.####", CultureInfo.InvariantCulture);
        var pageH = (height * 72f / (dpiY > 0 ? dpiY : 96f)).ToString("0.####", CultureInfo.InvariantCulture);
        var image = TiffCodecs.DeflateEncode(CmykConverter.FromBgra(bgra, icc));
        var useIcc = icc is { Length: > 128 } && TiffCmykService.IsCmykIcc(icc);
        var iccStream = useIcc ? TiffCodecs.DeflateEncode(icc!) : null;
        var colorSpace = useIcc ? "[/ICCBased 6 0 R]" : "/DeviceCMYK";
        var content = Encoding.ASCII.GetBytes($"q {pageW} 0 0 {pageH} 0 0 cm /Im0 Do Q");

        using var ms = new MemoryStream();
        var offsets = new List<long>();

        void Write(string text) => ms.Write(Encoding.ASCII.GetBytes(text));
        void Obj(int id, string dict, byte[]? data = null)
        {
            offsets.Add(ms.Position);
            Write($"{id} 0 obj\n<<{dict}" + (data is null ? ">>\nendobj\n" : $"/Length {data.Length}>>\nstream\n"));
            if (data is null) return;
            ms.Write(data);
            Write("\nendstream\nendobj\n");
        }

        Write("%PDF-1.5\n");
        Obj(1, "/Type/Catalog/Pages 2 0 R");
        Obj(2, "/Type/Pages/Kids[3 0 R]/Count 1");
        Obj(3, $"/Type/Page/Parent 2 0 R/MediaBox[0 0 {pageW} {pageH}]/Resources<</XObject<</Im0 4 0 R>>>>/Contents 5 0 R");
        Obj(4, $"/Type/XObject/Subtype/Image/Width {width}/Height {height}/ColorSpace {colorSpace}/BitsPerComponent 8/Filter/FlateDecode", image);
        Obj(5, "", content);
        if (iccStream is not null) Obj(6, "/N 4/Alternate/DeviceCMYK/Filter/FlateDecode", iccStream);

        var xref = ms.Position;
        Write($"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) Write($"{o:D10} 00000 n \n");
        Write($"trailer\n<</Size {offsets.Count + 1}/Root 1 0 R>>\nstartxref\n{xref}\n%%EOF\n");

        ms.Position = 0;
        ms.CopyTo(stream);
    }
}
