using Avalonia;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class PenTool : ITool
{
    private static PixelPoint? _lastPixel;

    public void OnPointerExited(ICanvasContext ctx)
    {

    }

    public void OnPointerMoved(ICanvasContext ctx) => Paint(ctx);

    public void OnPointerPressed(ICanvasContext ctx) => Paint(ctx);

    public void OnPointerReleased(ICanvasContext ctx) => _lastPixel = null;

    private static void Paint(ICanvasContext ctx)
    {
        var layer = ctx.LayerManager.ActiveLayer;
        if (ctx.HoverPixel is null || layer is null || layer.PixelData is null) return;

        // Do not forget to change it with tool width and height
        var current = ctx.HoverPixel.Value;
        var from = _lastPixel ?? current;

        var pixels = LineHelper.GetLine(from, current);

        var dirtyRect = new Rect(ctx.HoverPixel.Value.X, ctx.HoverPixel.Value.Y, 1, 1);

        foreach (var pixel in pixels)
        {
            BitmapService.BrushSquare(layer.PixelData, layer.Width, new Rect(pixel.X, pixel.Y, 1, 1), ctx.PickedColor);
            dirtyRect = dirtyRect.Union(new Rect(pixel.X, pixel.Y, 1, 1));
        }

        _lastPixel = current;
        ToolManager.InvalidatePixelData(ctx, layer, dirtyRect);
    }
}
