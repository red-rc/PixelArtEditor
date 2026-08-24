using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.Helpers;

public static class CanvasHelper
{
    public static (int bmpW, int bmpH, int offsetX, int offsetY) GetBitmapRenderInfo(ICanvasContext context)
    {
        var bmpW = (int)(context.Model.Width * context.Scale);
        var bmpH = (int)(context.Model.Height * context.Scale);

        var offsetX = (int)((context.Bounds.Width - bmpW) / 2 + context.Offset.X);
        var offsetY = (int)((context.Bounds.Height - bmpH) / 2 + context.Offset.Y);

        return (bmpW, bmpH, offsetX, offsetY);
    }

    public static PixelPoint? GetPixelCoord(ICanvasContext context, Visual relativeTo, PointerEventArgs e)
    {
        var pos = e.GetPosition(relativeTo);
        var (bmpW, bmpH, offsetX, offsetY) = GetBitmapRenderInfo(context);

        var relX = pos.X - offsetX;
        var relY = pos.Y - offsetY;

        if (relX < 0 || relY < 0 || relX >= bmpW || relY >= bmpH) return null;

        var px = (int)Math.Floor(relX / context.Scale);
        var py = (int)Math.Floor(relY / context.Scale);

        return new PixelPoint(px, py);
    }

    public static Color GetHighlightColor(Color color)
    {
        var factor = color.A == 0 ? 0 : (Math.Max(Math.Max(color.R, color.G), color.B) <= 127 ? 255 : 0);

        return new Color(
            65,
            (byte)factor,
            (byte)factor,
            (byte)factor);
    }
}
