using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.LayerPanel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class LayerPanelVM : ReactiveObject
{
    private LayerManager? _layerManager;
    public ObservableCollection<LayerItem> LayerItems { get; } = [];
    private int _originalWidth;
    private int _originalHeight;

    public ReactiveCommand<RxVoid, RxVoid> AddCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> RemoveCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CreateGroupCommand { get; }


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
            if (e.PropertyName != nameof(ISettingsManager.Theme)) return;
            foreach (var item in LayerItems)
                item.RefreshIcons();
        };

        AddCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null) return;

            var newLayer = new LayerModel(
                _originalWidth,
                _originalHeight,
                new byte[_originalWidth * _originalHeight * 4],
                $"Layer {_layerManager.Layers.Count + 1}"
            );

            var targetIndex = _layerManager.ActiveLayer is null ? 0 : Math.Max(0, _layerManager.Layers.IndexOf(_layerManager.ActiveLayer));
            _layerManager.Layers.Insert(targetIndex, newLayer);

            SelLayerItem = LayerItems.FirstOrDefault(x => x.Layer == newLayer);

        });
        RemoveCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null || _layerManager.Layers.Count == 0) return;
            if (SelLayerItems is null || SelLayerItems.Count == 0) return;

            var activeLayerItem = LayerItems.FirstOrDefault(x => x.Layer == _layerManager.ActiveLayer);
            var activeLayerIndex = activeLayerItem is not null ? LayerItems.IndexOf(activeLayerItem) : -1;

            foreach (var layerItem in SelLayerItems.ToList())
                _layerManager.Layers.Remove(layerItem.Layer);

            if ((_layerManager.ActiveLayer is null || !_layerManager.Layers.Contains(_layerManager.ActiveLayer)) 
                && LayerItems.Count > 0 && activeLayerIndex >= 0)
            {
                activeLayerIndex = Math.Min(activeLayerIndex, LayerItems.Count - 1);

                SelLayerItem = LayerItems[activeLayerIndex];
            }
        });
        CreateGroupCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null || _layerManager.Layers.Count == 0) return;
            // Implementation for creating a group
        });
    }

    public void SetLayerManager(LayerManager? layerManager)
    {
        if (layerManager is null)
        {
            ClearCurrentManager();
            return;
        }

        if (_layerManager == layerManager) return;
        _layerManager = layerManager;

        _layerManager.Layers.CollectionChanged += OnLayersChanged;

        foreach (var layer in _layerManager.Layers)
        {
            LayerItems.Add(new LayerItem(layer));
            _originalWidth = layer.Width;
            _originalHeight = layer.Height;
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
                    LayerItems.Remove(layerItem);
            }
        }
    }

    private void ClearCurrentManager()
    {
        if (_layerManager is not null && _layerManager.Layers is not null)
            _layerManager.Layers.CollectionChanged -= OnLayersChanged;

        _layerManager = null;
        SelLayerItem = null;
        LayerItems.Clear();
    }
}