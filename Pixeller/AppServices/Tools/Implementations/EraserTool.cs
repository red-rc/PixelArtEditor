using Avalonia;
using Avalonia.Media;
using Pixeller.AppServices.Bitmap;
using Pixeller.Models.Canvas;
using Pixeller.Models.Tools;

namespace Pixeller.AppServices.Tools.Implementations;

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
        if (ctx.HoverPixel is null || layer is null || layer.Data is null) return;

        // Do not forget to change it to tool width and height
        var dirtyRect = new PixelRect(ctx.HoverPixel.Value.X, ctx.HoverPixel.Value.Y, 1, 1);

        BitmapService.BrushSquare(layer.Data, layer.Width, dirtyRect, Colors.Transparent);
        ToolManager.InvalidatePixelData(ctx, layer, dirtyRect);
    }
}
