using System.Collections.ObjectModel;
using PixelArtEditor.Models.Canvas;

namespace PixelArtEditor.AppServices.Canvas;

public class LayerManager
{
    public ObservableCollection<LayerModel> Layers { get; } = [];
    public LayerModel? ActiveLayer { get; set; }

    public LayerModel InitializeFirstLayer(int width, int height, byte[] pixelData, string layerName, bool isEmpty)
    {
        Layers.Clear();

        if (layerName == "")
            layerName = $"{LocalizationService.Get("Layer")} 1";

        var layer = new LayerModel(width, height, BitmapService.SwapRB(pixelData), layerName, isEmpty);
        Layers.Add(layer);
        ActiveLayer = layer;

        return layer;
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
            layer.NotifyPixelDataChanged();
        }
    }

    public byte[] GetCompositePixelData(int width, int height)
        => BitmapService.GetCompositePixelData(Layers, width, height);
}