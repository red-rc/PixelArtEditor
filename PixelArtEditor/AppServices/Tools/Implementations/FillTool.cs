using Avalonia;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class FillTool : ITool
{
    public void OnPointerExited(ICanvasContext ctx)
    {

    }

    public void OnPointerMoved(ICanvasContext ctx)
    {

    }

    public void OnPointerPressed(ICanvasContext ctx) => Fill(ctx);

    public void OnPointerReleased(ICanvasContext ctx)
    {

    }

    private static void Fill(ICanvasContext ctx)
    {
        var layer = ctx.LayerManager.ActiveLayer;
        if (layer is null || ctx.HoverPixel is null) return;

        var dirtyRect = BitmapService.FillSimilarPixels(
            layer.PixelData, 
            layer.Width, 
            ctx.HoverPixel.Value, 
            ctx.PickedColor);

        if (dirtyRect is Rect rect)
            ToolManager.InvalidatePixelData(ctx, layer, rect);
    }
}
