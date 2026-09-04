using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class CopyCmd(LayerPanelVM vm, ListBox layerListBox) : LayerCmdBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (Vm.SelLayerItems is null || Vm.SelLayerItems.Count == 0 || layerManager is null || !CanExecute) return;

        var ordered = GetOrdered(Vm.SelLayerItems);
        if (ordered.Count == 0) return;

        Vm.CopiedLayers = [.. ordered.Select(x => new LayerModel(
            x.Layer.Width,
            x.Layer.Height,
            (byte[])x.Layer.PixelData.Clone(),
            LayerNameHelper.GetLayerName(layerManager, x.Layer.Name),
            x.Layer.IsEmpty))];
    }
}
