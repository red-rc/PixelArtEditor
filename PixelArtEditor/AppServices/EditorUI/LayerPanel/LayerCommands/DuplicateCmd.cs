using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class DuplicateCmd(LayerPanelVM vm, ListBox layerListBox) : LayerCmdBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (layerManager is null || !CanExecute) return;

        var copiedLayers = Vm.CopiedLayers;

        new CopyCmd(Vm, LayerListBox).Execute(layerManager);
        new InsertCmd(Vm, LayerListBox).Execute(layerManager);

        Vm.CopiedLayers = copiedLayers;
    }
}
