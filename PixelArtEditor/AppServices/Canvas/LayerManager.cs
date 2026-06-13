using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PixelArtEditor.Models.Canvas;

namespace PixelArtEditor.AppServices.Canvas;

public class LayerManager
{
    public ObservableCollection<LayerModel> Layers { get; } = [];
    public LayerModel? ActiveLayer { get; set; }

    public event Action<LayerModel>? LayerAdded;
    public event Action<LayerModel>? LayerRemoved;

    public LayerManager()
    {
        Layers.CollectionChanged += OnCollectionChanged;
    }

    public void ResizeLayers(int newWidth, int newHeight)
    {
        foreach (var layer in Layers)
        {
            var resized = BitmapService.ResizePixelData(layer.PixelData, layer.Width, layer.Height, newWidth, newHeight);
            layer.Width = newWidth;
            layer.Height = newHeight;
            layer.RenderBitmap?.Dispose();
            layer.RenderBitmap = BitmapService.CreateBitmap(newWidth, newHeight, resized);
            layer.PixelData = resized;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (LayerModel layer in e.NewItems)
                LayerAdded?.Invoke(layer);

        if (e.OldItems is not null)
            foreach (LayerModel layer in e.OldItems)
                LayerRemoved?.Invoke(layer);

        if (ActiveLayer is null || !Layers.Contains(ActiveLayer))
            ActiveLayer = Layers.Count > 0 ? Layers[0] : null;
    }

    public byte[] GetCompositePixelData(int width, int height)
    {
        var result = new byte[width * height * 4];

        foreach (var layer in Layers)
        {
            if (!layer.IsVisible) continue;

            var src = layer.PixelData;
            var alpha = layer.Opacity;

            for (var i = 0; i < result.Length; i += 4)
            {
                var srcA = src[i + 3] / 255f * alpha;
                var dstA = result[i + 3] / 255f;

                var outA = srcA + dstA * (1f - srcA);
                if (outA <= 0f) continue;

                result[i + 0] = (byte)((src[i + 0] * srcA + result[i + 0] * dstA * (1f - srcA)) / outA);
                result[i + 1] = (byte)((src[i + 1] * srcA + result[i + 1] * dstA * (1f - srcA)) / outA);
                result[i + 2] = (byte)((src[i + 2] * srcA + result[i + 2] * dstA * (1f - srcA)) / outA);
                result[i + 3] = (byte)(outA * 255f);
            }
        }

        return result;
    }
}