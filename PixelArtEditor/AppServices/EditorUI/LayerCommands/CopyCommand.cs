using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class CopyCommand(LayerPanelVM vm, ListBox layerListBox) : LayerCommandBase(vm, layerListBox)
{
    // Can't be static, compilator is crazy
    public List<LayerModel> Execute(LayerManager? layerManager, IReadOnlyList<LayerItem> ordered)
    {
        if (ordered.Count == 0 || layerManager is null) return [];

        return [.. ordered.Select(x => new LayerModel(
            x.Layer.Width,
            x.Layer.Height,
            (byte[])x.Layer.PixelData.Clone(),
            LayerNameHelper.GetLayerName(layerManager, x.Layer.Name)))];
    }
}
