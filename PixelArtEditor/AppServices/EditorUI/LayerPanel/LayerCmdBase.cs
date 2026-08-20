using Avalonia.Controls;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel;

public abstract class LayerCmdBase(LayerPanelVM vm, ListBox layerListBox)
{
    protected readonly LayerPanelVM Vm = vm;
    protected readonly ListBox LayerListBox = layerListBox;

    protected bool CanExecute => Services.Navigation.GetViewModel() is EditorVM editorVM && !editorVM.IsTransforming;

    protected List<LayerItem> GetOrdered(IEnumerable<LayerItem> items)
    {
        var order = Vm.LayerItems.Select(x => x.Layer).ToList();
        return [.. items.OrderBy(x => order.IndexOf(x.Layer))];
    }

    protected void RestoreSelection(IEnumerable<LayerModel> layers)
    {
        LayerListBox.SelectedItems?.Clear();
        foreach (var layer in layers)
        {
            var item = Vm.LayerItems.FirstOrDefault(x => x.Layer == layer);
            if (item is not null) LayerListBox.SelectedItems?.Add(item);
        }
    }
}
