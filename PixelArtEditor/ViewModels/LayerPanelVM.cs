using DynamicData;
using DynamicData.Binding;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.LayerPanel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class LayerPanelVM : ReactiveObject
{
    private LayerManager? _layerManager;
    public ObservableCollectionExtended<LayerItem> LayerItems { get; } = [];
    private IDisposable? _layerManagerSubscription;

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
        Services.Settings.WhenAnyValue(x => x.Theme).Subscribe(_ =>
        {
            foreach (var item in LayerItems)
                item.RefreshIcons();
        });
        Services.Settings.WhenAnyValue(x => x.Language).Subscribe(_ =>
        {
            foreach (var item in LayerItems)
                item.RefreshTags();
        });
    }

    public void SetLayerManager(LayerManager? layerManager)
    {
        if (layerManager is null) return;

        _layerManagerSubscription?.Dispose();

        foreach (var item in LayerItems) 
            item.Dispose();

        LayerItems.Clear();
        SelLayerItem = null;
        _layerManager = layerManager;

        _layerManagerSubscription = _layerManager.Layers
            .ToObservableChangeSet()
            .Transform(layer => new LayerItem(layer))
            .OnItemRemoved(item => item.Dispose())
            .Bind(LayerItems)
            .Subscribe();

        SelLayerItem = LayerItems.FirstOrDefault();

        if (SelLayerItem is not null)
        {
            OriginalWidth = SelLayerItem.Layer.Width;
            OriginalHeight = SelLayerItem.Layer.Height;
        }
    }
}