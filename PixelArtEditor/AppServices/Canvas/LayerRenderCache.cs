using Avalonia;

namespace PixelArtEditor.AppServices.Canvas;

public class LayerRenderCache
{
    public bool RenderBitmapDirty;
    public PixelRect? DirtyRect;
}