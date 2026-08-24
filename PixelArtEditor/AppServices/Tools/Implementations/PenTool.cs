using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class PenTool : ITool
{
    public void OnPointerExited(ICanvasContext ctx)
    {

    }

    public void OnPointerMoved(ICanvasContext ctx) => Paint(ctx);

    public void OnPointerPressed(ICanvasContext ctx) => Paint(ctx);

    public void OnPointerReleased(ICanvasContext ctx)
    {

    }

    private static void Paint(ICanvasContext ctx)
    {
        if (ctx.HoverPixel is null) return;
        ToolManager.UpdatePixelData(ctx, ctx.HoverPixel.Value.X, ctx.HoverPixel.Value.Y, ctx.PickedColor);
    }
}
