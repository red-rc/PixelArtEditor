using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class LayerItemVM(LayerModel layer, IBrush background) : ReactiveObject
{
    public LayerModel Layer { get; } = layer;
    public string LayerName { get; } = layer.Name;

    private bool _isVisible = layer.IsVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private IBrush _background = background;
    public IBrush Background
    {
        get => _background;
        set => this.RaiseAndSetIfChanged(ref _background, value);
    }
}

public class LayerPanelVM : ReactiveObject
{
    public ObservableCollection<LayerItemVM> Layers { get; } = [];

    public void SetLayerManager(LayerManager layerManager)
    {
        Layers.Clear();

        if (layerManager is null) return;

        foreach (var layer in layerManager.Layers)
            Layers.Add(new LayerItemVM(layer, GetLayerBackground(layer, layerManager)));

        layerManager.Layers.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (LayerModel layer in e.NewItems)
                    Layers.Add(new LayerItemVM(layer, GetLayerBackground(layer, layerManager)));

            if (e.OldItems is not null)
                foreach (LayerModel layer in e.OldItems)
                {
                    var vm = Layers.FirstOrDefault(x => x.Layer == layer);
                    if (vm is not null) Layers.Remove(vm);
                }
        };
    }

    private static SolidColorBrush GetLayerBackground(LayerModel layer, LayerManager manager)
    {
        var color = layer == manager.ActiveLayer
            ? Application.Current?.Resources["PrimaryPressedColor"] as Color? ?? Colors.Blue : Colors.Transparent;

        return new SolidColorBrush(color);
    }
}