using Avalonia.Interactivity;
using PixelArtEditor.Models.Canvas;
using System.Collections.ObjectModel;

namespace PixelArtEditor.Models.LayerPanel;

public interface ILayerPanelContext
{
    void DeleteClick(object? sender, RoutedEventArgs e);
    void DuplicateClick(object? sender, RoutedEventArgs e);
    void GroupClick(object? sender, RoutedEventArgs e);
    void RenameClick(object? sender, RoutedEventArgs e);

    LayerModel? GetActiveLayer();
    ObservableCollection<LayerModel>? GetLayers();
    ObservableCollection<LayerModel> GetSelLayers();
}
