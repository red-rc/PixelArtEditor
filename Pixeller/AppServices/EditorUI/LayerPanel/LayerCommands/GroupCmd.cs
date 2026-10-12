using Avalonia.Controls;
using Pixeller.AppServices.Canvas;
using Pixeller.ViewModels;

namespace Pixeller.AppServices.EditorUI.LayerPanel.LayerCommands;

public class GroupCmd(LayerPanelVM vm, ListBox layerListBox, TopLevel topLevel) : LayerCmdBase(vm, layerListBox, topLevel)
{
    public void Execute(LayerManager? layerManager)
    {
        if (layerManager is null || layerManager.Layers.Count == 0 || !CanExecute) return;
    }
}
