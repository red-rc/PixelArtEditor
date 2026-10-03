using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System.Linq;

namespace PixelArtEditor.Helpers;

public static class LayerPasteHelper
{
    public static void InsertPasted(PixelModel model, LayerManager layerManager, LayerPanelVM vm)
    {
        var (targetW, targetH) = BitmapService.FitToCanvas(model.Width, model.Height, vm.OriginalWidth, vm.OriginalHeight);

        if (targetW != model.Width || targetH != model.Height)
            model.Data = BitmapService.ResizePixelDataScaled(model.Data, model.Width, model.Height, targetW, targetH);

        model.Data = BitmapService.CenterOnCanvas(model.Data, targetW, targetH, vm.OriginalWidth, vm.OriginalHeight);

        var newLayer = new LayerModel(
            vm.OriginalWidth,
            vm.OriginalHeight,
            model.Data,
            model.Name ?? $"{LocalizationService.Get("Layer")} {layerManager.Layers.Count + 1}");

        var activeLayerItem = vm.LayerItems.FirstOrDefault(x => x.Layer == layerManager.ActiveLayer);
        var index = activeLayerItem is not null ? vm.LayerItems.IndexOf(activeLayerItem) : 0;

        layerManager.Layers.Insert(index, newLayer);
        vm.SelLayerItem = vm.LayerItems.FirstOrDefault(x => x.Layer == newLayer);

        if (Services.Navigation.GetViewModel() is not EditorVM editorvm) return;
        editorvm.NotifyLayersPasted([newLayer]);
    }
}