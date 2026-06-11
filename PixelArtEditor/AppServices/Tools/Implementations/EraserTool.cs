using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools.Implementations;

public class EraserTool : ITool
{
    public void OnPointerExited(ICanvasContext context)
    {

    }

    public void OnPointerMoved(ICanvasContext context) => Erase(context);

    public void OnPointerPressed(ICanvasContext context) => Erase(context);

    public void OnPointerReleased(ICanvasContext context)
    {

    }

    private static void Erase(ICanvasContext context)
    {
        if (context.HoverPixel is null) return;
        ToolManager.UpdatePixelData(context, context.HoverPixel.Value.X, context.HoverPixel.Value.Y, Colors.Transparent);
    }
}
