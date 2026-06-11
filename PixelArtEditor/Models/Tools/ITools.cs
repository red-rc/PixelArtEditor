using PixelArtEditor.Models.Canvas;

namespace PixelArtEditor.Models.Tools;

public interface ITool
{
    public void OnPointerMoved(ICanvasContext context);
    public void OnPointerPressed(ICanvasContext context);
    public void OnPointerExited(ICanvasContext context);
    public void OnPointerReleased(ICanvasContext context);
}
