using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PixelArtEditor.AppServices.EditorUI.LayerPanel;
using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.AppServices.Canvas;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.EditorUI;

public class ClipboardImageService
{
    public static async Task<PixelModel?> GetModel(IClipboard? clipboard)
    {
        if (clipboard is null) return null;

        using var data = await clipboard.TryGetDataAsync();
        if (data is null) return null;

        var file = await data.TryGetFileAsync();

        if (file is IStorageFile f)
            return await ImageImportService.GetPixelModelFromFile(f, false);

        using var bitmap = await data.TryGetBitmapAsync();
        if (bitmap is null) return null;

        using var ms = new MemoryStream();

        bitmap.Save(ms, new PngBitmapEncoderOptions());
        ms.Position = 0;

        return await ImageImportService.GetPixelModelFromStream(ms, showError: false);
    }

    public static async Task<List<LayerModel>?> GetLayers(IClipboard? clipboard)
    {
        if (clipboard is null) return null;

        using var data = await clipboard.TryGetDataAsync();
        if (data is null) return null;

        var item = data.Items.FirstOrDefault(i => i.Formats.Contains(LayerClipboardSerializer.FormatId));
        if (item is null) return null;

        if (await item.TryGetRawAsync(LayerClipboardSerializer.FormatId) is not byte[] bytes) return null;

        var layers = await Task.Run(() => LayerClipboardSerializer.Deserialize(bytes));
        return layers is { Count: > 0 } ? layers : null;
    }

    public static async Task<(PixelModel Model, LayerManager Layers)?> GetLayerManager(IClipboard? clipboard)
    {
        var layers = await GetLayers(clipboard);
        if (layers is null) return null;

        var first = layers[0];
        var manager = new LayerManager();
        foreach (var l in layers) manager.Layers.Add(l);
        manager.ActiveLayer = first;

        var model = new PixelModel
        {
            Name = first.Name,
            Width = first.Width,
            Height = first.Height,
            ColorMode = ColorMode.RGBA,
            BitDepth = BitDepth.Bit8,
            AlphaFormat = AlphaFormat.Straight,
            Data = await Task.Run(() => manager.GetCompositePixelData(first.Width, first.Height))
        };

        return (model, manager);
    }
}