using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace PixelArtEditor.ViewModels;

public class LayerItemVM : ReactiveObject
{
    public LayerModel Layer { get; }
    public string LayerName { get; }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public ReactiveCommand<Unit, Unit> HideCommand { get; }

    public LayerItemVM(LayerModel layer)
    {
        Layer = layer;
        LayerName = layer.Name;
        _isVisible = layer.IsVisible;

        HideCommand = ReactiveCommand.Create(() =>
        {
            IsVisible = !IsVisible;
        });
    }
}

public class LayerPanelVM : ReactiveObject
{
    public List<LayerItemVM> Layers { get; } = [];

    public LayerPanelVM(LayerManager layerManager)
    {
        for (var i = 0; i < layerManager.Layers.Count; i++)
        {
            Layers.Add(new LayerItemVM(layerManager.Layers[i]));
        }
    }
}