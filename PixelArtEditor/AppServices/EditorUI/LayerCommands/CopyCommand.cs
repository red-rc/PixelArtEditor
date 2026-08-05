using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class CopyCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (Vm.SelLayerItems is null || Vm.SelLayerItems.Count == 0 || layerManager is null) return;

        var ordered = GetOrdered(Vm.SelLayerItems);
        if (ordered.Count == 0) return;

        Vm.CopiedLayers = [.. ordered.Select(x => new LayerModel(
            x.Layer.Width,
            x.Layer.Height,
            (byte[])x.Layer.PixelData.Clone(),
            LayerNameHelper.GetLayerName(layerManager, x.Layer.Name)))];
    }
}
