using Avalonia;
using Avalonia.Media;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;

namespace PixelArtEditor.Models.LayerPanel;

public class LayerItem(LayerModel layer) : ReactiveObject
{
    public LayerModel Layer { get; } = layer;

    private string _layerName = layer.Name;
    public string LayerName
    {
        get => _layerName;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && value.Length < 256)
                this.RaiseAndSetIfChanged(ref _layerName, value);
        }
    }

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
}
