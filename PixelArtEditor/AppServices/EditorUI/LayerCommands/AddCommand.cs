using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class AddCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (layerManager is null) return;

        var newLayer = new LayerModel(
            Vm.OriginalWidth,
            Vm.OriginalHeight,
            new byte[Vm.OriginalWidth * Vm.OriginalHeight * 4],
            $"Layer {layerManager.Layers.Count + 1}"
        );

        var targetIndex = layerManager.ActiveLayer is null ? 0 : Math.Max(0, layerManager.Layers.IndexOf(layerManager.ActiveLayer));
        layerManager.Layers.Insert(targetIndex, newLayer);

        Vm.SelLayerItem = Vm.LayerItems.FirstOrDefault(x => x.Layer == newLayer);
    }
}
