using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class GroupCmd(LayerPanelVM vm, ListBox layerListBox) : LayerCmdBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager)
    {
        if (layerManager is null || layerManager.Layers.Count == 0 || !CanExecute) return;
    }
}
