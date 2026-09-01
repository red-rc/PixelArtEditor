using Avalonia.Threading;
using PixelArtEditor.Models.Canvas;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.Canvas;

public static class PreviewService
{
    public static void EnsurePreviewBitmap(ICanvasContext ctx, LayerModel layer, Action invalidate, int bmpW, int bmpH)
    {
        if (!ctx.RenderCache.TryGetValue(layer, out var cache)
            || layer.RenderBitmap is null
            || layer.PixelData is null) return;

        if (!cache.PreviewDirty && layer.PreviewBitmap != null
            && layer.PreviewBitmap.PixelSize.Width == bmpW
            && layer.PreviewBitmap.PixelSize.Height == bmpH) return;

        RequestPreview(ctx, layer, invalidate, bmpW, bmpH);
    }

    private static void RequestPreview(ICanvasContext ctx, LayerModel layer, Action invalidate, int width, int height)
    {
        if (!ctx.RenderCache.TryGetValue(layer, out var cache)) return;

        cache.PreviewCts?.Cancel();
        cache.PreviewCts = new CancellationTokenSource();
        var token = cache.PreviewCts.Token;
        var thisCts = cache.PreviewCts;

        Task.Run(() =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(width * height * 4);

            try
            {
                BitmapService.DownscaleNearest(layer.PixelData, ctx.Model.Width, ctx.Model.Height, buffer, width, height, token);
                if (token.IsCancellationRequested) return;

                Dispatcher.UIThread.Post(() =>
                {
                    if (ReferenceEquals(cache.PreviewCts, thisCts))
                    {
                        layer.PreviewBitmap?.Dispose();
                        layer.PreviewBitmap = BitmapService.CreateBitmap(width, height, buffer);
                        cache.PreviewDirty = false;
                        invalidate();
                    }

                    ArrayPool<byte>.Shared.Return(buffer);
                }, DispatcherPriority.Render);
            }
            catch { try { ArrayPool<byte>.Shared.Return(buffer); } catch { } }
        }, CancellationToken.None);
    }
}