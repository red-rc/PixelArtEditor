using Pixeller.AppServices.Bitmap;
using Pixeller.Models.Canvas;
using Pixeller.Models.Tools;

namespace Pixeller.AppServices.Tools.Implementations;

public class ColorPickerTool : ITool
{
    public void OnPointerExited(ICanvasContext ctx)
    {

    }

    public void OnPointerMoved(ICanvasContext ctx)
    {

    }

    public void OnPointerPressed(ICanvasContext ctx) => PickColor(ctx);

    public void OnPointerReleased(ICanvasContext ctx)
    {

    }

    private static void PickColor(ICanvasContext ctx)
    {
        var layer = ctx.LayerManager.ActiveLayer;
        if (layer is null || ctx.HoverPixel is null) return;

        ctx.PickedColor = BitmapService.GetPixelColor(layer.Data, layer.Width, ctx.HoverPixel.Value);
    }
}
