using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class InsertCmd(LayerPanelVM vm, ListBox layerListBox) : LayerCmdBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        var layers = Vm.CopiedLayers;
        if (layers.Count == 0 || layerManager is null || !CanExecute) return;

        Debug.WriteLine($"Inserting {layers.Count} layers");

        var activeLayerItem = Vm.LayerItems.FirstOrDefault(x => x.Layer == layerManager.ActiveLayer);
        var index = activeLayerItem is not null ? Vm.LayerItems.IndexOf(activeLayerItem) : -1;

        index = Math.Max(index, 0);

        List<LayerModel> newLayers = [];
        for (var i = 0; i < layers.Count; i++)
        {
            var layer = new LayerModel(
                layers[i].Width, 
                layers[i].Height,
                (byte[])layers[i].PixelData.Clone(), 
                Helpers.LayerNameHelper.GetLayerName(layerManager, layers[i].Name));

            layerManager.Layers.Insert(index + i, layer);
            newLayers.Add(layer);
        }

        RestoreSelection(newLayers);
    }
}
