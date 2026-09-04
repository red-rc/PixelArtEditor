using Avalonia;
using System.Threading;

namespace PixelArtEditor.AppServices.Canvas;

public class LayerRenderCache
{
    public bool RenderBitmapDirty;
    public bool PreviewDirty;
    public Rect? DirtyRect;
    public Rect? RenderRect;
    public CancellationTokenSource? PreviewCts;
}
