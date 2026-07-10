using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI;

public class LayerReorderService(LayerPanelVM vm, ListBox layerListBox)
{
    private readonly LayerPanelVM _vm = vm;
    private readonly ListBox _layerListBox = layerListBox;
    public void MoveSelected(LayerManager? layerManager, bool toTop)
    {
        if (layerManager?.Layers is null || layerManager.Layers.Count <= 1) return;

        var selected = _layerListBox.SelectedItems?.OfType<LayerItemVM>().ToList();
        if (selected is null || selected.Count == 0) return;

        var ordered = selected
            .OrderBy(x => _vm!.LayerItems.IndexOf(x))
            .Select(x => x.Layer)
            .ToList();

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

    public void MoveSelectedStep(LayerManager? layerManager, int direction)
    {
        if (layerManager?.Layers is null || layerManager.Layers.Count <= 1) return;

        var selected = _layerListBox.SelectedItems?.OfType<LayerItemVM>().ToList();
        if (selected is null || selected.Count == 0) return;

        var ordered = selected
            .OrderBy(x => _vm!.LayerItems.IndexOf(x))
            .Select(x => x.Layer)
            .ToList();

        var indices = ordered.Select(l => layerManager.Layers.IndexOf(l)).ToList();

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

    private void RestoreSelection(IEnumerable<LayerModel> layers)
    {
        var items = _vm!.LayerItems.Where(li => layers.Contains(li.Layer)).ToList();

        _layerListBox.SelectedItems?.Clear();
        foreach (var item in items)
            _layerListBox.SelectedItems?.Add(item);
    }
}
