using Avalonia;
using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using System.ComponentModel;

namespace PixelArtEditor.Models.LayerPanel;

public class LayerItem: ReactiveObject
{
    public LayerModel Layer { get; }

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    private string _layerName;
    public string LayerName
    {
        get => _layerName;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && value.Length < 256)
                this.RaiseAndSetIfChanged(ref _layerName, value);
        }
    }

    private bool _isVisible;
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

    private bool _isLocked;
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

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => this.RaiseAndSetIfChanged(ref _isEditing, value);
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

    public LayerItem(LayerModel layer)
    {
        Layer = layer;
        _renderData = new PreviewData(layer.Width, layer.Height, layer.RenderBitmap, null);
        _layerName = layer.Name;
        _isVisible = layer.IsVisible;
        _isLocked = layer.IsLocked;

        Layer.PropertyChanged += OnLayerPropertyChanged;
    }

    private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayerModel.PixelData))
        {
            RenderData.Width = Layer.Width;
            RenderData.Height = Layer.Height;
            RenderData.Bitmap = Layer.RenderBitmap;
            RenderData.NotifyPropertyChanged();
        }
    }

    public void Unsubscribe() => Layer.PropertyChanged -= OnLayerPropertyChanged;
}
