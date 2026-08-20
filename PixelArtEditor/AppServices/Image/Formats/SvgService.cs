using SkiaSharp;
using Svg.Skia;
using System.IO;

namespace PixelArtEditor.AppServices.Image.Formats;

public static class SvgService
{
    // Рендерить SVG у RGBA32 byte[]. targetWidth/Height — якщо 0, беремо натуральний розмір SVG.
    public static (byte[] data, int width, int height)? RenderToRgba32(Stream stream, int targetWidth = 0, int targetHeight = 0)
    {
        using var svg = new SKSvg();
        var picture = svg.Load(stream);
        if (picture is null) return null;

        var srcW = picture.CullRect.Width;
        var srcH = picture.CullRect.Height;
        if (srcW <= 0 || srcH <= 0) return null;

        var width = targetWidth > 0 ? targetWidth : (int)System.Math.Ceiling(srcW);
        var height = targetHeight > 0 ? targetHeight : (int)System.Math.Ceiling(srcH);

        var scaleX = width / srcW;
        var scaleY = height / srcH;

        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(scaleX, scaleY);
            canvas.DrawPicture(picture);
        }

        var data = bitmap.Bytes; // вже RGBA8888, unpremul
        return (data, width, height);
    }
}