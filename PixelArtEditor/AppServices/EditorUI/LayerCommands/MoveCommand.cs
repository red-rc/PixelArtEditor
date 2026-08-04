using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class MoveCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    public void Execute(LayerManager? layerManager, bool toTop)
    {
        if (layerManager?.Layers is null || layerManager.Layers.Count <= 1) return;

        var selected = LayerListBox.SelectedItems?.OfType<LayerItem>().ToList();
        if (selected is null || selected.Count == 0) return;

        var ordered = GetOrdered(selected).Select(x => x.Layer).ToList();

        if (toTop)
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                var current = layerManager.Layers.IndexOf(ordered[i]);
                if (current != i)
                    layerManager.Layers.Move(current, i);
            }
        }
        else
        {
            for (var i = ordered.Count - 1; i >= 0; i--)
            {
                var target = layerManager.Layers.Count - 1 - (ordered.Count - 1 - i);
                var current = layerManager.Layers.IndexOf(ordered[i]);
                if (current != target)
                    layerManager.Layers.Move(current, target);
            }
        }

        RestoreSelection(ordered);
    }
}
