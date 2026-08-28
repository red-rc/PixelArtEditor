using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;
using System;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class RemoveCmd(LayerPanelVM vm, ListBox layerListBox) : LayerCmdBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (layerManager is null || layerManager.Layers.Count == 0 || !CanExecute) return;
        if (Vm.SelLayerItems is null || Vm.SelLayerItems.Count == 0) return;

        var activeLayerItem = Vm.LayerItems.FirstOrDefault(x => x.Layer == layerManager.ActiveLayer);
        var index = activeLayerItem is not null ? Vm.LayerItems.IndexOf(activeLayerItem) : -1;

        foreach (var layerItem in Vm.SelLayerItems.ToList())
        {
            if (layerItem.Layer == layerManager.ActiveLayer)
                layerManager.ActiveLayer = null;

            layerManager.Layers.Remove(layerItem.Layer);
        }

        if ((layerManager.ActiveLayer is null || !layerManager.Layers.Contains(layerManager.ActiveLayer))
            && Vm.LayerItems.Count > 0)
        {
            index = Math.Clamp(index, 0, Vm.LayerItems.Count - 1);
            Vm.SelLayerItem = Vm.LayerItems[index];
        }
    }
}
