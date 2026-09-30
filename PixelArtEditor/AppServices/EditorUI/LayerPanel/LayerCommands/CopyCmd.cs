using Avalonia.Controls;
using Avalonia.Input;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;

public class CopyCmd(LayerPanelVM vm, ListBox layerListBox, TopLevel topLevel) : LayerCmdBase(vm, layerListBox, topLevel)
{
    public async Task Execute(LayerManager? layerManager)
    {
        if (Vm.SelLayerItems is null || Vm.SelLayerItems.Count == 0 || layerManager is null || !CanExecute) return;

        var ordered = GetOrdered(Vm.SelLayerItems);
        if (ordered.Count == 0) return;

        List<LayerModel> layers = [.. ordered.Select(x => new LayerModel(
            x.Layer.Width,
            x.Layer.Height,
            (byte[])x.Layer.Data.Clone(),
            LayerNameHelper.GetLayerName(layerManager, x.Layer.Name),
            x.Layer.IsEmpty)
        {
            Opacity = x.Layer.Opacity,
            IsVisible = x.Layer.IsVisible,
            IsLocked = x.Layer.IsLocked
        })];

        if (TopLevel.Clipboard is not { } clipboard) return;

        var item = DataTransferItem.Create(LayerClipboardSerializer.FormatId, LayerClipboardSerializer.Serialize(layers));
        var dataTransfer = new DataTransfer();
        dataTransfer.Add(item);

        await clipboard.SetDataAsync(dataTransfer);
    }
}