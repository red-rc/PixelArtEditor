using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class PenTool : ITool
{
    public void OnPointerExited(ICanvasContext context)
    {

    }

    public void OnPointerMoved(ICanvasContext context) => Paint(context);

    public void OnPointerPressed(ICanvasContext context) => Paint(context);

    public void OnPointerReleased(ICanvasContext context)
    {

    }

    private static void Paint(ICanvasContext context)
    {
        if (context.HoverPixel is null) return;
        ToolManager.UpdatePixelData(context, context.HoverPixel.Value.X, context.HoverPixel.Value.Y, context.PickedColor);
    }
}
