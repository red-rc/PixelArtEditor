using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        set 
        {
            Layer.IsVisible = value;
            this.RaiseAndSetIfChanged(ref _isVisible, value);
            this.RaisePropertyChanged(nameof(VisibleIconSource));
        }
    }

    private bool _isLocked = layer.IsLocked;
    public bool IsLocked
    {
        get => _isLocked;
        set 
        {
            Layer.IsLocked = value;
            this.RaiseAndSetIfChanged(ref _isLocked, value);
            this.RaisePropertyChanged(nameof(LockedIconSource));
        }
    }

    public IImage? VisibleIconSource => IsVisible
    ? Application.Current?.Resources["ShowIcon"] as IImage
    : Application.Current?.Resources["HideIcon"] as IImage;

    public IImage? LockedIconSource => IsLocked
        ? Application.Current?.Resources["LockIcon"] as IImage
        : Application.Current?.Resources["UnlockIcon"] as IImage;

    public void RefreshIcons()
    {
        this.RaisePropertyChanged(nameof(VisibleIconSource));
        this.RaisePropertyChanged(nameof(LockedIconSource));
    }
}

public class LayerPanelVM : ReactiveObject
{
    private LayerManager? _layerManager;
    public ObservableCollection<LayerItemVM> LayerItems { get; } = [];
    private int _originalWidth;
    private int _originalHeight;

    public ReactiveCommand<Unit, Unit> AddCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateGroupCommand { get; }
    public ReactiveCommand<Unit, Unit> UpCommand { get; }
    public ReactiveCommand<Unit, Unit> DownCommand { get; }


    private LayerItemVM? _selectedLayer;
    public LayerItemVM? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (value == _selectedLayer || value is null) return;
            _layerManager!.ActiveLayer = value.Layer;
            this.RaiseAndSetIfChanged(ref _selectedLayer, value);
        }
    }

    private ObservableCollection<LayerItemVM>? _selectedLayers;
    public ObservableCollection<LayerItemVM>? SelectedLayers
    {
        get => _selectedLayers;
        set
        {
            if (value == _selectedLayers || value is null) return;
            this.RaiseAndSetIfChanged(ref _selectedLayers, value);
        }
    }

    public LayerPanelVM()
    {
        Services.Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(ISettingsService.Theme)) return;
            foreach (var item in LayerItems)
                item.RefreshIcons();
        };

        AddCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null) return;

            _layerManager.Layers.Add(new LayerModel(
                _originalWidth,
                _originalHeight,
                new byte[_originalWidth * _originalHeight * 4],
                $"Layer {_layerManager.Layers.Count + 1}"
            ));
        });
        RemoveCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null || _layerManager.Layers.Count == 0) return;
            if (SelectedLayers is null || SelectedLayers.Count == 0) return;

            foreach (var layerItem in SelectedLayers.ToList())
                _layerManager.Layers.Remove(layerItem.Layer);
        });
        DuplicateCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null || _layerManager.Layers.Count == 0) return;
            if (_layerManager.ActiveLayer is null) return;

            var name = _layerManager.ActiveLayer.Name + " - Copy";

            if (_layerManager.Layers.Any(l => l.Name == name))
            {
                var copyIndex = 1;
                while (_layerManager.Layers.Any(l => l.Name == $"{name} ({copyIndex})"))
                {
                    copyIndex++;
                }
                name = $"{name} ({copyIndex})";
            }

            _layerManager.Layers.Add(new LayerModel(
                _layerManager.ActiveLayer.Width,
                _layerManager.ActiveLayer.Height,
                (byte[])_layerManager.ActiveLayer.PixelData.Clone(),
                name
            ));
        });
        CreateGroupCommand = ReactiveCommand.Create(() => 
        { 
            if (_layerManager?.Layers is null || _layerManager.Layers.Count == 0) return;
            // Implementation for creating a group
        });
        UpCommand = ReactiveCommand.Create(() =>
        {
            if (_layerManager?.Layers is null || _layerManager.Layers.Count <= 1 || _layerManager.ActiveLayer is null) return;

            var layer = _layerManager.ActiveLayer;
            _layerManager.Layers.Move(_layerManager.Layers.IndexOf(layer), 0);
        });
        DownCommand = ReactiveCommand.Create(() =>
        {
            if (_layerManager?.Layers is null || _layerManager.Layers.Count <= 1 || _layerManager.ActiveLayer is null) return;

            var layer = _layerManager.ActiveLayer;
            _layerManager.Layers.Move(_layerManager.Layers.IndexOf(layer), _layerManager.Layers.Count - 1);
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
            LayerItems.Add(new LayerItemVM(layer));
            _originalWidth = layer.Width;
            _originalHeight = layer.Height;
        }

        SelectedLayer = LayerItems.FirstOrDefault();
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
            foreach (LayerModel layer in e.NewItems)
                LayerItems.Add(new LayerItemVM(layer));
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
        SelectedLayer = null;
        LayerItems.Clear();
    }
}