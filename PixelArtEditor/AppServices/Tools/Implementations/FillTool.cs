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

        BitmapService.FillSimilarPixels(layer.RenderBitmap, 
            layer.PixelData, layer.Width, layer.Height,
            ctx.HoverPixel.Value, ctx.PickedColor);
    }
}
