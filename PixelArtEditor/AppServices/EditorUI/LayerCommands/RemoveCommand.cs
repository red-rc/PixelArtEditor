using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;
using System;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class RemoveCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (layerManager is null || layerManager.Layers.Count == 0) return;
        if (Vm.SelLayerItems is null || Vm.SelLayerItems.Count == 0) return;

        var activeLayerItem = Vm.LayerItems.FirstOrDefault(x => x.Layer == layerManager.ActiveLayer);
        var index = activeLayerItem is not null ? Vm.LayerItems.IndexOf(activeLayerItem) : -1;

        foreach (var layerItem in Vm.SelLayerItems.ToList())
            layerManager.Layers.Remove(layerItem.Layer);

        if ((layerManager.ActiveLayer is null || !layerManager.Layers.Contains(layerManager.ActiveLayer))
            && Vm.LayerItems.Count > 0 && index >= 0)
        {
            index = Math.Min(index, Vm.LayerItems.Count - 1);
            Vm.SelLayerItem = Vm.LayerItems[index];
        }
    }
}
