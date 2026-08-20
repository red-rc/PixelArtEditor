using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class MoveStepCmd(LayerPanelVM vm, ListBox layerListBox) : LayerCmdBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager, int direction)
    {
        if (layerManager is null || layerManager.Layers.Count <= 1 || !CanExecute) return;

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
