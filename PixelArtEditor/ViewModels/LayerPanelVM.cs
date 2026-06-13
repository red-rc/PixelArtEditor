using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace PixelArtEditor.ViewModels;

public class LayerItemVM(LayerModel layer) : ReactiveObject
{
    public LayerModel Layer { get; } = layer;
    public string LayerName { get; } = layer.Name;

    private bool _isVisible = layer.IsVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private IBrush _background = new SolidColorBrush(Colors.Transparent);
    public IBrush Background
    {
        get => _background;
        set => this.RaiseAndSetIfChanged(ref _background, value);
    }
}

public class LayerPanelVM : ReactiveObject
{
    private LayerManager? _layerManager;
    public ObservableCollection<LayerItemVM> LayerItems { get; } = [];
    public ReactiveCommand<Unit, Unit> AddLayerCommand { get; }

    private LayerItemVM? _selectedLayer;
    public LayerItemVM? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (value == _selectedLayer || value is null) return;

            _selectedLayer?.Background = new SolidColorBrush(Colors.Transparent);

            _layerManager!.ActiveLayer = value.Layer;
            value.Background = GetLayerBackground(value.Layer, _layerManager!);
            this.RaiseAndSetIfChanged(ref _selectedLayer, value);
        }
    }

    public LayerPanelVM()
    {
        AddLayerCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null || _layerManager.Layers.Count == 0) return;
            _layerManager.Layers.Add(new LayerModel(
                _layerManager.Layers[0].Width,
                _layerManager.Layers[0].Height,
                new byte[_layerManager.Layers[0].PixelData.Length],
                $"Layer {_layerManager.Layers.Count + 1}"
            ));
        });
    }

    public void SetLayerManager(LayerManager layerManager)
    {
        _layerManager = layerManager;
        LayerItems.Clear();

        if (layerManager is null) return;

        foreach (var layer in layerManager.Layers)
            LayerItems.Add(new LayerItemVM(layer));

        layerManager.Layers.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
            {
                foreach (LayerModel layer in e.NewItems)
                    LayerItems.Insert(0, new LayerItemVM(layer));
            }
            if (e.OldItems is not null)
                foreach (LayerModel layer in e.OldItems)
                {
                    var vm = LayerItems.FirstOrDefault(x => x.Layer == layer);
                    if (vm is not null) LayerItems.Remove(vm);
                }
        };

        SelectedLayer = LayerItems[0];
    }

    private static SolidColorBrush GetLayerBackground(LayerModel layer, LayerManager manager)
    {
        var color = layer == manager.ActiveLayer
            ? Application.Current?.Resources["PrimaryColor"] as Color? ?? Colors.Blue : Colors.Transparent;

        return new SolidColorBrush(color);
    }
}