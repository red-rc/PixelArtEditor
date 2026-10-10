using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.Models.Canvas;
using ReactiveUI.SourceGenerators;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace PixelArtEditor.Models.LayerPanel;

public partial class LayerItem: ReactiveObject, IDisposable
{
    public LayerModel Layer { get; }

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && value.Length < 256)
                this.RaiseAndSetIfChanged(ref _name, value);
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

    [Reactive] public partial bool IsEditing { get; set; }

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

    [Reactive] public partial string HideShowTag { get; set; }
    [Reactive] public partial string LockUnlockTag { get; set; }

    private string GetHideShowTag() => IsVisible ? LocalizationService.Get("Hide") : LocalizationService.Get("Show");
    private string GetLockUnlockTag() => IsLocked ? LocalizationService.Get("Unlock") : LocalizationService.Get("Lock");

    public void RefreshTags()
    {
        HideShowTag = GetHideShowTag();
        LockUnlockTag = GetLockUnlockTag();
    }

    private const int ThumbSize = 36;

    public LayerItem(LayerModel layer)
    {
        Layer = layer;

        _renderData = new PreviewData(layer.Width, layer.Height, CreateThumb(), null);

        _name = layer.Name;
        _isVisible = layer.IsVisible;
        _isLocked = layer.IsLocked;

        HideShowTag = GetHideShowTag();
        LockUnlockTag = GetLockUnlockTag();

        Observable.Merge(
            Layer.WhenAnyValueUnsafe(x => x.ThumbDirtyRect).Where(rect => rect is not null).Select(_ => Unit.Default),
            Layer.WhenAnyValueUnsafe(x => x.Data).Skip(1).Select(_ => Unit.Default))
            .Subscribe(_ =>
        {
            UpdateThumb();
            RenderData.RaisePropertyChanged(nameof(PreviewData.Bitmap));
        });
    }

    private WriteableBitmap CreateThumb()
    {
        var scale = Math.Min(1.0, (double)ThumbSize / Math.Max(Layer.Width, Layer.Height));
        var thumbW = Math.Max(1, (int)(Layer.Width * scale));
        var thumbH = Math.Max(1, (int)(Layer.Height * scale));

        return BitmapService.DownscaleBox(Layer.Data, Layer.Width, Layer.Height, thumbW, thumbH);
    }

    private void UpdateThumb()
    {
        var bitmap = RenderData.Bitmap;
        var dirty = Layer.ThumbDirtyRect;
        Layer.ThumbDirtyRect = null;

        if (bitmap is null || dirty is null || RenderData.Width != Layer.Width || RenderData.Height != Layer.Height)
        {
            RenderData.Width = Layer.Width;
            RenderData.Height = Layer.Height;

            RenderData.Bitmap = CreateThumb();
            bitmap?.Dispose();

            return;
        }

        var region = BitmapService.ToThumbRegion(dirty.Value, Layer.Width, Layer.Height,
            bitmap.PixelSize.Width, bitmap.PixelSize.Height);

        BitmapService.DownscaleBoxRegion(Layer.Data, Layer.Width, Layer.Height, bitmap, region);
        RenderData.Bitmap = bitmap;
    }

    public void Dispose()
    {
        RenderData.Bitmap?.Dispose();
        RenderData.Bitmap = null;
        GC.SuppressFinalize(this);
    }
}