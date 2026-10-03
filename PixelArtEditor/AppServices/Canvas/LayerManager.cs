using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.ObjectModel;

namespace PixelArtEditor.AppServices.Canvas;

public class LayerManager
{
    public ObservableCollection<LayerModel> Layers { get; } = [];
    public LayerModel? ActiveLayer { get; set; }

    public LayerModel InitializeFirstLayer(int width, int height, byte[] pixelData, string layerName)
    {
        Layers.Clear();

        if (layerName == "")
            layerName = $"{LocalizationService.Get("Layer")} 1";

        var layer = new LayerModel(width, height, pixelData, layerName);
        Layers.Add(layer);
        ActiveLayer = layer;

        return layer;
    }

    public void ResizeLayers(int newWidth, int newHeight)
    {
        if (Layers.Count == 0) return;

        foreach (var layer in Layers)
        {
            if (newWidth == layer.Width && newHeight == layer.Height) 
                continue;

            var newData = BitmapService.ResizePixelData(layer.Data, layer.Width, layer.Height, newWidth, newHeight);
            layer.Resize(newWidth, newHeight, newData);
        }
    }

    public void ToIndexed(Palette palette) =>
        ApplyToLayers(data => BitmapService.QuantizeToPalette(data, palette, palette.Dither ?? false));
    public void ToGrayscale() =>
        ApplyToLayers(data => ImageConverterService.ToGrayscale(ImageConverterService.ToRgb(data)));
    public void ToGrayscaleAlpha() => ApplyToLayers(ImageConverterService.ToGrayscale);
    public void ToRgb() => ApplyToLayers(ImageConverterService.ToRgb);
    public void ToRedGreen() => ApplyToLayers(ImageConverterService.ToRedGreen);
    public void ToAlpha() => ApplyToLayers(ImageConverterService.ToAlpha);  

    private void ApplyToLayers(Func<byte[], byte[]> transform)
    {
        foreach (var layer in Layers)
        {
            layer.Data = transform(layer.Data);

            layer.Tiles?.Dispose();
            layer.Tiles = null!;
            layer.ThumbDirtyRect = null;
        }
    }

    public byte[] GetCompositePixelData(int width, int height)
        => BitmapService.GetCompositePixelData(Layers, width, height);
}