using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class DuplicateCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (Vm.SelLayerItems is null || Vm.SelLayerItems.Count == 0 || layerManager is null) return;

        var ordered = GetOrdered(Vm.SelLayerItems);
        var sourceIndex = layerManager.Layers.IndexOf(ordered[0].Layer);

        var copies = new CopyCommand(Vm, LayerListBox).Execute(layerManager, ordered);
        new InsertCommand(Vm, LayerListBox).Execute(layerManager, copies, sourceIndex);
    }
}
