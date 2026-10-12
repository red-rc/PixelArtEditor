using Avalonia.Controls;
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

        var model = await ClipboardImageService.GetModel(clipboard);
        if (model is not null)
        {
            LayerPasteHelper.InsertPasted(model, layerManager, Vm);
            return;
        }

        var layers = await ClipboardImageService.GetLayers(clipboard);
        if (layers is null) return;

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
