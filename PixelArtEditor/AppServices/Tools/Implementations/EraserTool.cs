using Avalonia.Media;
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
        if (ctx.HoverPixel is null) return;
        ToolManager.UpdatePixelData(ctx, ctx.HoverPixel.Value.X, ctx.HoverPixel.Value.Y, Colors.Transparent);
    }
}
