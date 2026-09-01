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
        cache.PreviewDirty = false;
    }

    private static void RequestPreview(ICanvasContext ctx, LayerModel layer, Action invalidate, int width, int height)
    {
        if (!ctx.RenderCache.TryGetValue(layer, out var cache)) return;

        cache.PreviewCts?.Cancel();
        cache.PreviewCts = new CancellationTokenSource();
        var token = cache.PreviewCts.Token;
        var thisCts = cache.PreviewCts;

        var modelWidth = ctx.Model.Width;
        var modelHeight = ctx.Model.Height;
        var pixelData = layer.PixelData;

        Task.Run(() =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(width * height * 4);

            try
            {
                BitmapService.DownscaleNearest(pixelData, modelWidth, modelHeight, buffer, width, height, token);
                if (token.IsCancellationRequested)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (ReferenceEquals(cache.PreviewCts, thisCts))
                        {
                            var old = layer.PreviewBitmap;
                            layer.PreviewBitmap = BitmapService.CreateBitmap(width, height, buffer);
                            Dispatcher.UIThread.Post(() => old?.Dispose(), DispatcherPriority.Background);
                            invalidate();
                        }
                    }
                    finally { ArrayPool<byte>.Shared.Return(buffer); }
                }, DispatcherPriority.Render);
            }
            catch { try { ArrayPool<byte>.Shared.Return(buffer); } catch { } }
        }, CancellationToken.None);
    }
}