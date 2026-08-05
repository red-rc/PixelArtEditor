using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class DuplicateCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (layerManager is null) return;

        var copiedLayers = Vm.CopiedLayers;

        new CopyCommand(Vm, LayerListBox).Execute(layerManager);
        new InsertCommand(Vm, LayerListBox).Execute(layerManager);

        Vm.CopiedLayers = copiedLayers;
    }
}
