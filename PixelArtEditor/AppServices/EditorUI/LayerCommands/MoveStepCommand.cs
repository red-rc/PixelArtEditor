using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class MoveStepCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager, int direction)
    {
        if (layerManager?.Layers is null || layerManager.Layers.Count <= 1) return;

        var selected = LayerListBox.SelectedItems?.OfType<LayerItem>().ToList();
        if (selected is null || selected.Count == 0) return;

        var ordered = GetOrdered(selected).Select(x => x.Layer);

        var indices = ordered.Select(layerManager.Layers.IndexOf).ToList();

        if (direction < 0)
        {
            if (indices[0] == 0) return;

            foreach (var layer in ordered)
            {
                var index = layerManager.Layers.IndexOf(layer);
                if (ordered.Contains(layerManager.Layers[index - 1])) continue;

                layerManager.Layers.Move(index, index - 1);
            }
        }
        else
        {
            if (indices[^1] == layerManager.Layers.Count - 1) return;

            foreach (var layer in ordered.AsEnumerable().Reverse())
            {
                var index = layerManager.Layers.IndexOf(layer);
                if (ordered.Contains(layerManager.Layers[index + 1])) continue;

                layerManager.Layers.Move(index, index + 1);
            }
        }

        RestoreSelection(ordered);
    }
}
