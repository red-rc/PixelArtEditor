using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class ColorPickerTool : ITool
{
    public void OnPointerExited(ICanvasContext context)
    {

    }

    public void OnPointerMoved(ICanvasContext context)
    {

    }

    public void OnPointerPressed(ICanvasContext context) => PickColor(context);

    public void OnPointerReleased(ICanvasContext context)
    {

    }

    private static void PickColor(ICanvasContext context)
    {
        var layer = context.LayerManager.ActiveLayer;
        if (layer is null || context.HoverPixel is null) return;

        context.PickedColor = BitmapService.GetPixelColor(layer.PixelData, layer.Width, context.HoverPixel.Value);
    }
}
