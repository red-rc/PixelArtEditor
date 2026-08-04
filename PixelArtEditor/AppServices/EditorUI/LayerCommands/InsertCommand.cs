using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class InsertCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager, IReadOnlyList<LayerModel> layers, int index)
    {
        if (layers.Count == 0 || layerManager is null) return;

        for (var i = 0; i < layers.Count; i++)
            layerManager.Layers.Insert(index + i, layers[i]);

        RestoreSelection(layers);
    }
}
