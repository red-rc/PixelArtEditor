using Avalonia;

namespace Pixeller.AppServices.Canvas;

public class LayerRenderCache
{
    public bool RenderBitmapDirty;
    public PixelRect? DirtyRect;
}