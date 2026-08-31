using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices;
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
            HideShowTag = GetHideShowTag();
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
            LockUnlockTag = GetLockUnlockTag();
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

    private string _hideShowTag;
    public string HideShowTag
    {
        get => _hideShowTag;
        set => this.RaiseAndSetIfChanged(ref _hideShowTag, value);
    }

    private string _lockUnlockTag;
    public string LockUnlockTag
    {
        get => _lockUnlockTag;
        set => this.RaiseAndSetIfChanged(ref _lockUnlockTag, value);
    }

    private string GetHideShowTag()
        => IsVisible ? LocalizationService.Get("Hide") : LocalizationService.Get("Show");
    private string GetLockUnlockTag()
       => IsLocked ? LocalizationService.Get("Unlock") : LocalizationService.Get("Lock");


    public void RefreshTags()
    {
        HideShowTag = GetHideShowTag();
        LockUnlockTag = GetLockUnlockTag();
    }

    public LayerItem(LayerModel layer)
    {
        Layer = layer;
        _renderData = new PreviewData(layer.Width, layer.Height, layer.RenderBitmap, null);
        _layerName = layer.Name;
        _isVisible = layer.IsVisible;
        _isLocked = layer.IsLocked;

        _hideShowTag = GetHideShowTag();
        _lockUnlockTag = GetLockUnlockTag();

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
