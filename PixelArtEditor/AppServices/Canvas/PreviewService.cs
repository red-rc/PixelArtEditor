using Avalonia.Threading;
using PixelArtEditor.Models.Canvas;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.Canvas;

public static class PreviewService
{
    private static void DownscaleNearest(byte[] src, int srcW, int srcH, byte[] dst, int dstW, int dstH, CancellationToken token)
    {
        for (int y = 0; y < dstH; y++)
        {
            if ((y & 7) == 0 && token.IsCancellationRequested) return;

            var srcY = (int)((long)y * srcH / dstH);
            if (srcY >= srcH) srcY = srcH - 1;

            var dstRow = y * dstW * 4;
            var srcRow = srcY * srcW * 4;

            for (int x = 0; x < dstW; x++)
            {
                var srcX = (int)((long)x * srcW / dstW);
                if (srcX >= srcW) srcX = srcW - 1;
                Buffer.BlockCopy(src, srcRow + srcX * 4, dst, dstRow + x * 4, 4);
            }
        }
    }

    // PreviewService
    public static void EnsurePreviewBitmap(ICanvasContext context, LayerModel layer, Action invalidate, double bmpW, double bmpH)
    {
        if (!context.RenderCache.TryGetValue(layer, out var cache)) return;
        if (layer.RenderBitmap is null || layer.PixelData is null) return;

        const int minPreviewSize = 128;
        var targetW = (int)Math.Ceiling(bmpW);
        var targetH = (int)Math.Ceiling(bmpH);

        if (!cache.PreviewDirty && layer.PreviewBitmap != null
            && layer.PreviewBitmap.PixelSize.Width == targetW
            && layer.PreviewBitmap.PixelSize.Height == targetH) return;

        if (targetW >= context.Model.Width && targetH >= context.Model.Height)
        {
            layer.PreviewBitmap = null;
            cache.PreviewDirty = false;
            return;
        }

        RequestPreview(context, layer, invalidate, Math.Max(minPreviewSize, targetW), Math.Max(minPreviewSize, targetH));
        cache.PreviewDirty = false;
    }

    private static void RequestPreview(ICanvasContext context, LayerModel layer, Action invalidate, int width, int height)
    {
        if (!context.RenderCache.TryGetValue(layer, out var cache)) return;

        cache.PreviewCts?.Cancel();
        cache.PreviewCts = new CancellationTokenSource();
        var token = cache.PreviewCts.Token;
        var thisCts = cache.PreviewCts;
        var size = width * height * 4;

        Task.Run(() =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                DownscaleNearest(layer.PixelData!, context.Model.Width, context.Model.Height, buffer, width, height, token);
                if (token.IsCancellationRequested) return;

                Dispatcher.UIThread.Post(() =>
                {
                    if (ReferenceEquals(cache.PreviewCts, thisCts))
                    {
                        var old = layer.PreviewBitmap;
                        layer.PreviewBitmap = BitmapService.CreateBitmap(width, height, buffer);
                        Dispatcher.UIThread.Post(() => old?.Dispose(), DispatcherPriority.Background);
                        invalidate();
                    }
                    ArrayPool<byte>.Shared.Return(buffer);
                }, DispatcherPriority.Normal);
            }
            catch { try { ArrayPool<byte>.Shared.Return(buffer); } catch { } }
        }, CancellationToken.None);
    }
}
