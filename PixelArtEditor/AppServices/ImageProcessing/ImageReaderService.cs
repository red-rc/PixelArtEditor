using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO;
using Color = Avalonia.Media.Color;

namespace PixelArtEditor.AppServices.ImageProcessing;

public static class ImageReaderService
{
    public record IndexedImageMetadata(Palette Palette, BitDepth BitDepth);

    public static (float dpiX, float dpiY) GetDpi(ImageMetadata meta)
    {
        var x = meta.HorizontalResolution > 0 ? (float)meta.HorizontalResolution : 96f;
        var y = meta.VerticalResolution > 0 ? (float)meta.VerticalResolution : 96f;
        return (x, y);
    }

    /// <summary>
    /// Converts any image into a 32-bit BGRA byte array (native to the UI).
    /// </summary>
    private static PixelModel ReadAsBgra32(Image image, ColorMode mode, BitDepth bitDepth, AlphaFormat alpha = AlphaFormat.None,
        ColorSpace colorSpace = ColorSpace.sRGB, Palette? palette = null)
    {
        var (dpiX, dpiY) = GetDpi(image.Metadata);
        var rgbaData = new byte[image.Width * image.Height * 4];

        if (image is Image<Rgba32> rgba32)
            rgba32.CopyPixelDataTo(rgbaData);
        else
        {
            using var converted = image.CloneAs<Rgba32>();
            converted.CopyPixelDataTo(rgbaData);
        }

        var bgraData = BitmapService.SwapRB(rgbaData);

        return new PixelModel
        {
            Width = image.Width,
            Height = image.Height,
            Mode = mode,
            BitDepth = bitDepth,
            Alpha = alpha,
            ColorSpace = colorSpace,
            Palette = palette,
            DpiX = dpiX,
            DpiY = dpiY,
            Data = bgraData
        };
    }

    public static PixelModel ReadRgb24(Image<Rgb24> image)
        => ReadAsBgra32(image, ColorMode.RGB, BitDepth.Bit8, AlphaFormat.None, ColorSpace.sRGB);

    public static PixelModel ReadRgb48(Image<Rgb48> image)
        => ReadAsBgra32(image, ColorMode.RGB, BitDepth.Bit16, AlphaFormat.None, ColorSpace.sRGB);

    public static PixelModel ReadBgr565(Image<Bgr565> image)
        => ReadAsBgra32(image, ColorMode.RGB, BitDepth.RGB565, AlphaFormat.None, ColorSpace.Linear);

    public static PixelModel ReadRgba32(Image<Rgba32> image)
        => ReadAsBgra32(image, ColorMode.RGBA, BitDepth.Bit8, AlphaFormat.Straight, ColorSpace.sRGB);

    public static PixelModel ReadRgba64(Image<Rgba64> image)
        => ReadAsBgra32(image, ColorMode.RGBA, BitDepth.Bit16, AlphaFormat.Straight, ColorSpace.sRGB);

    public static PixelModel ReadL8(Image<L8> image)
        => ReadAsBgra32(image, ColorMode.Grayscale, BitDepth.Bit8, AlphaFormat.None, ColorSpace.sRGB);

    public static PixelModel ReadL16(Image<L16> image)
        => ReadAsBgra32(image, ColorMode.Grayscale, BitDepth.Bit16, AlphaFormat.None, ColorSpace.sRGB);

    public static PixelModel ReadLa16(Image<La16> image)
        => ReadAsBgra32(image, ColorMode.Grayscale, BitDepth.Bit8, AlphaFormat.Straight, ColorSpace.sRGB);

    public static PixelModel ReadLa32(Image<La32> image)
        => ReadAsBgra32(image, ColorMode.Grayscale, BitDepth.Bit16, AlphaFormat.Straight, ColorSpace.sRGB);

    public static PixelModel? ReadIndexed(Image<Rgba32> image, Palette palette, BitDepth bitDepth)
    {
        if (palette.Colors.Count == 0 || palette.Colors.Count > 256) return null;

        return ReadAsBgra32(image, ColorMode.Indexed, bitDepth, AlphaFormat.Straight, ColorSpace.sRGB, palette);
    }

    public static PixelModel ReadFallback(Image image)
        => ReadAsBgra32(image, ColorMode.RGBA, BitDepth.Bit8, AlphaFormat.Straight, ColorSpace.sRGB);

    public static IndexedImageMetadata? ExtractIndexedMetadata(Image image, MemoryStream ms)
    {
        var pngMeta = image.Metadata.GetPngMetadata();
        if (pngMeta?.ColorType == PngColorType.Palette && pngMeta.ColorTable.HasValue)
        {
            var colorTable = pngMeta.ColorTable.Value;
            var colors = new List<Color>(colorTable.Length);
            for (var i = 0; i < colorTable.Length; i++)
            {
                var rgba = colorTable.Span[i].ToPixel<Rgba32>();
                colors.Add(Color.FromArgb(rgba.A, rgba.R, rgba.G, rgba.B));
            }

            if (colors.Count is > 0 and <= 256)
            {
                var bitDepth = colors.Count switch
                {
                    <= 2 => BitDepth.Bit1,
                    <= 4 => BitDepth.Bit2,
                    <= 16 => BitDepth.Bit4,
                    _ => BitDepth.Bit8
                };
                return new IndexedImageMetadata(new Palette(colors), bitDepth);
            }
        }

        var gifMeta = image.Metadata.GetGifMetadata();
        var gifColorTable = gifMeta?.GlobalColorTable;

        if (!gifColorTable.HasValue && image.Frames.Count > 0)
        {
            var frameMeta = image.Frames.RootFrame.Metadata.GetGifMetadata();
            gifColorTable = frameMeta?.LocalColorTable;
        }

        if (gifColorTable.HasValue)
        {
            var colorTable = gifColorTable.Value;
            var colors = new List<Color>(colorTable.Length);
            for (var i = 0; i < colorTable.Length; i++)
            {
                var rgba = colorTable.Span[i].ToPixel<Rgba32>();
                colors.Add(Color.FromArgb(rgba.A, rgba.R, rgba.G, rgba.B));
            }

            if (colors.Count is > 0 and <= 256)
            {
                var bitDepth = colors.Count <= 16 ? BitDepth.Bit4 : BitDepth.Bit8;
                return new IndexedImageMetadata(new Palette(colors), bitDepth);
            }
        }

        var bmpMeta = image.Metadata.GetBmpMetadata();
        if (bmpMeta.BitsPerPixel is BmpBitsPerPixel.Pixel1 or BmpBitsPerPixel.Pixel2 or BmpBitsPerPixel.Pixel4 or BmpBitsPerPixel.Pixel8)
        {
            var bmpPalette = BmpHelper.ExtractBmpPalette(ms);
            if (bmpPalette.Count is > 0 and <= 256)
            {
                var bitDepth = bmpMeta.BitsPerPixel switch
                {
                    BmpBitsPerPixel.Pixel1 => BitDepth.Bit1,
                    BmpBitsPerPixel.Pixel2 => BitDepth.Bit2,
                    BmpBitsPerPixel.Pixel4 => BitDepth.Bit4,
                    _ => BitDepth.Bit8
                };
                return new IndexedImageMetadata(new Palette(bmpPalette), bitDepth);
            }
        }

        return null;
    }
}