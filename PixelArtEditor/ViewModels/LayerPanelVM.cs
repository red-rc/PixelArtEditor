using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.LayerPanel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class LayerPanelVM : ReactiveObject
{
    private LayerManager? _layerManager;
    public ObservableCollection<LayerItem> LayerItems { get; } = [];
    public int OriginalWidth;
    public int OriginalHeight;


    private LayerItem? _selLayerItem;
    public LayerItem? SelLayerItem
    {
        get => _selLayerItem;
        set
        {
            if (value == _selLayerItem) return;

            this.RaiseAndSetIfChanged(ref _selLayerItem, value);

            if (value is not null && _layerManager is not null)
                _layerManager.ActiveLayer = value.Layer;

            this.RaisePropertyChanged(nameof(Opacity));
        }
    }

    private ObservableCollection<LayerItem>? _selLayerItems;
    public ObservableCollection<LayerItem>? SelLayerItems
    {
        get => _selLayerItems;
        set
        {
            if (value == _selLayerItems || value is null) return;
            this.RaiseAndSetIfChanged(ref _selLayerItems, value);
        }
    }

    public List<LayerModel> CopiedLayers = [];

    public byte Opacity
    {
        get => (byte)((_layerManager?.ActiveLayer?.Opacity ?? 1f) * 100);
        set
        {
            if (_layerManager?.ActiveLayer is null) return;
            _layerManager.ActiveLayer.Opacity = value / 100f;
            this.RaisePropertyChanged(nameof(Opacity));
        }
    }

    public LayerPanelVM()
    {
        Services.Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISettingsManager.Theme))
            {
                foreach (var item in LayerItems)
                    item.RefreshIcons();

            }

            if (e.PropertyName == nameof(ISettingsManager.Language))
            {
                foreach (var item in LayerItems)
                    item.RefreshTags();
            }
        };
    }

    public void SetLayerManager(LayerManager? layerManager)
    {
        if (layerManager is null) return;

        _layerManager?.Layers.CollectionChanged -= OnLayersChanged;

        _layerManager = layerManager;

        _layerManager.Layers.CollectionChanged += OnLayersChanged;

        SelLayerItem = null;
        LayerItems.Clear();

        foreach (var layer in _layerManager.Layers)
        {
            LayerItems.Add(new LayerItem(layer));
            OriginalWidth = layer.Width;
            OriginalHeight = layer.Height;
        }

        SelLayerItem = LayerItems.FirstOrDefault();
    }

    private void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Move)
        {
            LayerItems.Move(e.OldStartingIndex, e.NewStartingIndex);
            return;
        }

        if (e.NewItems is not null)
        {
            var index = e.NewStartingIndex;
            foreach (LayerModel layer in e.NewItems)
                LayerItems.Insert(index++, new LayerItem(layer));
        }

        if (e.OldItems is not null)
        {
            foreach (LayerModel layer in e.OldItems)
            {
                var layerItem = LayerItems.FirstOrDefault(x => x.Layer == layer);
                if (layerItem is not null)
                {
                    layerItem.Unsubscribe();
                    LayerItems.Remove(layerItem);
                }
            }
        }
    }
}