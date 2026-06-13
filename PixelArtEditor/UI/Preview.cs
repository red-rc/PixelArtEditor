using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;
using System.ComponentModel;

namespace PixelArtEditor.UI;

public class Preview : Control
{
    public static readonly StyledProperty<LayerModel> LayerProperty =
        AvaloniaProperty.Register<Preview, LayerModel>(nameof(Layer));

    public LayerModel Layer
    {
        get => GetValue(LayerProperty);
        set => SetValue(LayerProperty, value);
    }

    public static readonly StyledProperty<bool> IsAlreadyBgraProperty =
        AvaloniaProperty.Register<Preview, bool>(nameof(IsAlreadyBgra), defaultValue: false);

    public bool IsAlreadyBgra
    {
        get => GetValue(IsAlreadyBgraProperty);
        set => SetValue(IsAlreadyBgraProperty, value);
    }

    public static readonly StyledProperty<int?> SizeProperty =
        AvaloniaProperty.Register<Preview, int?>(nameof(Size));

    public int? Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    private ImageBrush? _checkerboardBrush;
    private WriteableBitmap? _renderBitmap;

    private byte[]? _pixelData;
    private int _initBitmapWidth;
    private int _initBitmapHeight;

    public Preview()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        LayerProperty.Changed.AddClassHandler<Preview>((sender, _) => OnLayerChanged());
    }

    private void OnLayerChanged()
    {
        if (Layer is not { Width: > 0, Height: > 0 }) return;

        if (_initBitmapWidth == 0 || _initBitmapHeight == 0)
        {
            _initBitmapWidth = Layer.Width;
            _initBitmapHeight = Layer.Height;
        }

        if (!ReferenceEquals(Layer.PixelData, _pixelData) || _pixelData is null)
        {
            _pixelData = Layer.PixelData;

            if (!IsAlreadyBgra && _pixelData.Length > 0)
                _pixelData = BitmapService.SwapRB(_pixelData);

            _renderBitmap?.Dispose();
            _renderBitmap = null;
        }

        if (_pixelData is not { Length: > 0 }) return;

        if (_renderBitmap is null)
        {
            _renderBitmap = BitmapService.CreateBitmap(Layer.Width, Layer.Height, _pixelData);
        }
        else if (_renderBitmap.PixelSize.Width != Layer.Width || _renderBitmap.PixelSize.Height != Layer.Height)
        {
            var resized = BitmapService.ResizePixelData(
                _pixelData,
                _initBitmapWidth, _initBitmapHeight,
                Layer.Width, Layer.Height);

            _renderBitmap.Dispose();
            _renderBitmap = BitmapService.CreateBitmap(Layer.Width, Layer.Height, resized);
        }

        Layer.PropertyChanged -= Layer_PropertyChanged;
        Layer.PropertyChanged += Layer_PropertyChanged;

        InvalidateVisual();
    }

    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LayerModel.PixelData))
            OnLayerChanged();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_renderBitmap is null) return;

        var ratio = (double)Layer.Width / Layer.Height;
        if (double.IsInfinity(ratio) || double.IsNaN(ratio)) ratio = 1;

        var size = Size ?? 200;
        var rect = size / ratio > size ? new Rect((double)(size - size * ratio) / 2, 0, size * ratio, size) : 
            new Rect(0, (double)(size - size / ratio) / 2, size, size / ratio);
        
        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(8, 8, BitmapService.CreateCheckerBoardPixelData(8, 8)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 64, 64, RelativeUnit.Absolute)
        };

        context.FillRectangle(_checkerboardBrush, rect);
        context.DrawImage(_renderBitmap, rect);
    }
}