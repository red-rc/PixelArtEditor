using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.Models.Canvas;
using System.Collections.ObjectModel;

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

        var layer = new LayerModel(width, height, pixelData, layerName, isEmpty);
        Layers.Add(layer);
        ActiveLayer = layer;

        return layer;
    }

    public void ResizeLayers(int newWidth, int newHeight)
    {
        foreach (var layer in Layers)
        {
            var resized = BitmapService.ResizePixelData(layer.Data, layer.Width, layer.Height, newWidth, newHeight);
            layer.Width = newWidth;
            layer.Height = newHeight;
            layer.RenderBitmap?.Dispose();
            layer.RenderBitmap = BitmapService.CreateBitmap(resized, newWidth, newHeight);
            layer.Data = resized;
            layer.NotifyPixelDataChanged();
        }
    }

    public void QuantizeToPalette(Palette palette)
    {
        foreach (var layer in Layers)
        {
            layer.Data = BitmapService.QuantizeToPalette(layer.Data, palette, palette.Dither ?? false);
            layer.RenderBitmap = null!;
            layer.NotifyPixelDataChanged();
        }
    }

    public byte[] GetCompositePixelData(int width, int height)
        => BitmapService.GetCompositePixelData(Layers, width, height);
}