using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class DuplicateCmd(LayerPanelVM vm, ListBox layerListBox, TopLevel topLevel) : LayerCmdBase(vm, layerListBox, topLevel)
{
    public async Task Execute(LayerManager? layerManager)
    {
        if (layerManager is null || !CanExecute) return;

        var layers = new CopyCmd(Vm, LayerListBox, TopLevel).GetLayers(layerManager);
        if (layers is null || layers.Count == 0) return;

        new PasteCmd(Vm, LayerListBox, TopLevel).InsertLayers(layers, layerManager);
    }
}