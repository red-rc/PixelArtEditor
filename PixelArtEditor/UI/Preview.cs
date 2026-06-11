using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.Canvas;

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
        LayerProperty.Changed.AddClassHandler<Preview>((sender, _) => sender.InvalidateVisual());
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Layer is not { Width: > 0, Height: > 0 }) return;

        if (_initBitmapWidth == 0 || _initBitmapHeight == 0)
        {
            _initBitmapWidth = Layer.Width;
            _initBitmapHeight = Layer.Height;
        }

        var ratio = (double)Layer.Width / Layer.Height;
        if (double.IsInfinity(ratio) || double.IsNaN(ratio)) ratio = 1;

        Size ??= 200;
        var rect = Size / ratio > Size ? new Rect((double)(Size - Size * ratio) / 2, 0, (double)Size * ratio, (double)Size) : 
            new Rect(0, (double)(Size - Size / ratio) / 2, (double)Size, (double)Size / ratio);
        
        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(8, 8, BitmapService.CreateCheckerBoardPixelData(8, 8)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 64, 64, RelativeUnit.Absolute)
        };

        if (!ReferenceEquals(Layer.PixelData, _pixelData) || _pixelData is null)
        {
            _pixelData = Layer.PixelData;
            if (!IsAlreadyBgra && _pixelData.Length > 0)
                BitmapService.RGBAToBGRA(_pixelData);
            _renderBitmap?.Dispose();
            _renderBitmap = null;
        }

        if (_pixelData is not { Length: > 0 }) return;

        if (_renderBitmap is not null)
        {
            if (_renderBitmap.Size.Width != Layer.Width || _renderBitmap.Size.Height != Layer.Height)
            {
                var bitmapData = BitmapService.ResizePixelData(
                    _pixelData,
                    _initBitmapWidth,
                    _initBitmapHeight,
                    Layer.Width, Layer.Height);

                _renderBitmap = BitmapService.CreateBitmap(Layer.Width, Layer.Height, bitmapData);
            }
        }
        else
        {
            _renderBitmap = BitmapService.CreateBitmap(Layer.Width, Layer.Height, _pixelData);
        }

        context.FillRectangle(_checkerboardBrush, rect);
        context.DrawImage(_renderBitmap, rect);
    }
}