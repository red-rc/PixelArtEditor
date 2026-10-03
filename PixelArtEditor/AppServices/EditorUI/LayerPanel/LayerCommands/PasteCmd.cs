using Avalonia.Controls;
using Avalonia.Input;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class PasteCmd(LayerPanelVM vm, ListBox layerListBox, TopLevel topLevel) : LayerCmdBase(vm, layerListBox, topLevel)
{
    public async Task Execute(LayerManager? layerManager)
    {
        if (layerManager is null || !CanExecute || TopLevel.Clipboard is not { } clipboard) return;

        using var data = await clipboard.TryGetDataAsync();
        if (data is null) return;

        var model = await ClipboardImageService.GetModel(clipboard);
        if (model is not null)
        {
            LayerPasteHelper.InsertPasted(model, layerManager, Vm);
            return;
        }

        var item = data.Items.FirstOrDefault(i => i.Formats.Contains(LayerClipboardSerializer.FormatId));
        if (item is null) return;

        if (await item.TryGetRawAsync(LayerClipboardSerializer.FormatId) is not byte[] bytes) return;

        var layers = await Task.Run(() => LayerClipboardSerializer.Deserialize(bytes));
        if (layers is null || layers.Count == 0) return;

        InsertLayers(layers, layerManager);
    }

    public void InsertLayers(List<LayerModel> layers, LayerManager layerManager)
    {
        var activeLayerItem = Vm.LayerItems.FirstOrDefault(x => x.Layer == layerManager.ActiveLayer);
        var idx = activeLayerItem is not null ? Vm.LayerItems.IndexOf(activeLayerItem) : -1;
        idx = Math.Max(idx, 0);

        List<LayerModel> newLayers = [];
        for (var i = 0; i < layers.Count; i++)
        {
            var layer = new LayerModel(
                layers[i].Width,
                layers[i].Height,
                (byte[])layers[i].Data.Clone(),
                LayerNameHelper.GetLayerName(layerManager, layers[i].Name),
                layers[i].IsEmpty)
            {
                Opacity = layers[i].Opacity,
                IsVisible = layers[i].IsVisible,
                IsLocked = layers[i].IsLocked
            };

            layerManager.Layers.Insert(idx + i, layer);
            newLayers.Add(layer);
        }

        RestoreSelection(newLayers);
    }
}