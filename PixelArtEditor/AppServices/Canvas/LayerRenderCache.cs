using System.Threading;

namespace PixelArtEditor.AppServices.Canvas;

public class LayerRenderCache
{
    public bool RenderBitmapDirty;
    public bool PreviewDirty;
    public CancellationTokenSource? PreviewCts;
}
