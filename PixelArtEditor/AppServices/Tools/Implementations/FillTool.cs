using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class FillTool : ITool
{
    public void OnPointerExited(ICanvasContext context)
    {

    }

    public void OnPointerMoved(ICanvasContext context)
    {

    }

    public void OnPointerPressed(ICanvasContext context) => Fill(context);

    public void OnPointerReleased(ICanvasContext context)
    {

    }

    private static void Fill(ICanvasContext context)
    {
        var layer = context.LayerManager.ActiveLayer;
        if (layer is null || context.HoverPixel is null) return;

        BitmapService.FillSimilarPixels(layer.RenderBitmap, layer.PixelData,
            layer.Width, layer.Height,
            context.HoverPixel.Value, context.PickedColor);
    }
}
