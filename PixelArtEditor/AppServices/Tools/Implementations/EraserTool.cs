using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class EraserTool : ITool
{
    public void OnPointerExited(ICanvasContext ctx)
    {

    }

    public void OnPointerMoved(ICanvasContext ctx) => Erase(ctx);

    public void OnPointerPressed(ICanvasContext ctx) => Erase(ctx);

    public void OnPointerReleased(ICanvasContext ctx)
    {

    }

    private static void Erase(ICanvasContext ctx)
    {
        var layer = ctx.LayerManager.ActiveLayer;
        if (ctx.HoverPixel is null || layer is null || layer.PixelData is null) return;

        // Do not forget to change it to tool width and height
        var dirtyRect = new Rect(ctx.HoverPixel.Value.X, ctx.HoverPixel.Value.Y, 1, 1);

        BitmapService.BrushSquare(layer.PixelData, layer.Width, dirtyRect, Colors.Transparent);
        ToolManager.InvalidatePixelData(ctx, layer, dirtyRect);
    }
}
