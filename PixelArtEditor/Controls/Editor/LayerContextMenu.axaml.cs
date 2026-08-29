using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.AppServices.Tools;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using Splat.ModeDetection;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PixelArtEditor.Controls.Editor
{
    public partial class LayerContextMenu : UserControl
    {
        private readonly ILayerPanelContext _ctx;
        private readonly Action _closeFlyout;

        public LayerContextMenu(ILayerPanelContext ctx, Action closeFlyout)
        {
            InitializeComponent();

            _ctx = ctx;
            _closeFlyout = closeFlyout;
        }

        private void RenameClick(object? sender, RoutedEventArgs e)
        {
            _ctx.RenameClick(sender, e);
            _closeFlyout();
        }

        private void DuplicateClick(object? sender, RoutedEventArgs e)
        {
            _ctx.DuplicateClick(sender, e);
            _closeFlyout();
        }

        private void GroupClick(object? sender, RoutedEventArgs e)
        {

        }

        private void MergeClick(object? sender, RoutedEventArgs e)
        {
            var activeLayer = _ctx.GetActiveLayer();
            var selLayers = _ctx.GetSelLayers();

            if (activeLayer is not null)
            {
                activeLayer.PixelData =
                    BitmapService.GetCompositePixelData(selLayers, activeLayer.Width, activeLayer.Height);

                //ToolManager.InvalidatePixelData(activeLayer, new Rect(0, 0, activeLayer.Width, activeLayer.Height), false);

                foreach (var item in selLayers)
                    if (item != activeLayer)
                        _ctx.GetLayers()?.Remove(item);
            }

            _closeFlyout();
        }

        private void TransformClick(object? sender, RoutedEventArgs e)
        {

        }

        private async void ExportClick(object? sender, RoutedEventArgs e)
        {
            var layer = _ctx.GetActiveLayer();
            if (layer is null) return;

            await ActionService.ShowExportWindowAsync(new PixelModel
            { 
                Name = layer.Name,
                Width = layer.Width,
                Height = layer.Height,
                Data = layer.PixelData
            });
        }

        private void FiltersClick(object? sender, RoutedEventArgs e)
        {

        }

        private void DeleteClick(object? sender, RoutedEventArgs e) => _ctx.DeleteClick(sender, e);
    }
}