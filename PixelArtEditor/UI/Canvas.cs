using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using PixelArtEditor.ViewModels;
using System;
using System.Buffers;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.UI;

public class Canvas : Control
{
    private static readonly ISettingsService _settings = Services.Settings;

    private readonly Pen _gridPen = new(new SolidColorBrush(_settings.GridColor));

    public static readonly StyledProperty<Vector2> OffsetProperty =
        AvaloniaProperty.Register<Canvas, Vector2>(nameof(Offset));

    public Vector2 Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }
    
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<Canvas, double>(nameof(Scale));

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }
    
    public static readonly StyledProperty<ToolType> SelectedToolProperty =
        AvaloniaProperty.Register<Canvas, ToolType>(nameof(SelectedTool));
    
    public ToolType SelectedTool
    {
        get => GetValue(SelectedToolProperty);
        set => SetValue(SelectedToolProperty, value);
    }

    public static readonly StyledProperty<CreateParams?> ParametersProperty =
        AvaloniaProperty.Register<Canvas, CreateParams?>(nameof(Parameters));

    public CreateParams? Parameters
    {
        get => GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public static readonly StyledProperty<WriteableBitmap?> ImportedBitmapProperty =
    AvaloniaProperty.Register<Canvas, WriteableBitmap?>(nameof(ImportedBitmap));

    public WriteableBitmap? ImportedBitmap
    {
        get => GetValue(ImportedBitmapProperty);
        set => SetValue(ImportedBitmapProperty, value);
    }

    private bool _isLeftPressed;

    private PixelPoint? _hoverPixel;
    private PixelPoint? HoverPixel
    {
        get => _hoverPixel;
        set
        {
            if (_hoverPixel == value) return;
            _hoverPixel = value;
            InvalidateVisual();
        }
    }

    public static readonly StyledProperty<Color> PickedColorProperty =
        AvaloniaProperty.Register<Canvas, Color>(nameof(PickedColor));
    
    public Color PickedColor
    {
        get => GetValue(PickedColorProperty);
        set => SetValue(PickedColorProperty, value);
    }
    
    private ImageBrush? _checkerboardBrush;
    
    private WriteableBitmap? _renderBitmap;
    public WriteableBitmap? GetBitmap() => _renderBitmap;

    public static readonly StyledProperty<PixelPoint?> CurrentPixelCoordProperty =
    AvaloniaProperty.Register<Canvas, PixelPoint?>(nameof(CurrentPixelCoord));

    public PixelPoint? CurrentPixelCoord
    {
        get => GetValue(CurrentPixelCoordProperty);
        set => SetValue(CurrentPixelCoordProperty, value);
    }

    private byte[]? _pixelData;
    private bool _renderBitmapDirty;

    private WriteableBitmap? _previewBitmap;
    private bool _previewDirty = true;
    private CancellationTokenSource? _previewCts;

    private short BitmapWidth => (short)(_renderBitmap?.PixelSize.Width ?? 0);
    private short BitmapHeight => (short)(_renderBitmap?.PixelSize.Height ?? 0);

    public Canvas()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

        this.GetObservable(ImportedBitmapProperty).Subscribe(bitmap =>
        {
            if (bitmap is null) return;

            _renderBitmap?.Dispose();

            _renderBitmap = bitmap;
            _pixelData = BitmapService.GetPixelData(bitmap);

            _renderBitmapDirty = true;
            InvalidateVisual();
        });
        this.GetObservable(ParametersProperty).Subscribe(param =>
        {
            if (param is null) return;

            _renderBitmap?.Dispose();

            _pixelData = BitmapService.CreatePixelData(param.Width, param.Height, param.BackgroundColor);
            _renderBitmap = BitmapService.CreateBitmap(param.Width, param.Height, _pixelData);

            _renderBitmapDirty = false;
            InvalidateVisual();
        });
        this.GetObservable(OffsetProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(ScaleProperty).Subscribe(_ => InvalidateVisual());

        _previewBitmap = null;
        _previewDirty = true;
    }

    private (Size bmpSize, Point offset) GetBitmapRenderInfo()
    {
        if (_renderBitmap is null) return (new Size(0, 0), new Point(0, 0));
        
        var bmpW = _renderBitmap.PixelSize.Width * Scale;
        var bmpH = _renderBitmap.PixelSize.Height * Scale;

        var offsetX = (Bounds.Width - bmpW) / 2 + Offset.X;
        var offsetY = (Bounds.Height - bmpH) / 2 + Offset.Y;

        return (new Size(bmpW, bmpH), new Point(offsetX, offsetY));
    }
    
    private PixelPoint? GetPixelCoord(PointerEventArgs e)
    {
        if (_renderBitmap is null) return null;

        var pos = e.GetPosition(this);

        var ((bmpW, bmpH), (offsetX, offsetY)) = GetBitmapRenderInfo();

        var relX = pos.X - offsetX;
        var relY = pos.Y - offsetY;
        
        if (relX < 0 || relY < 0 || relX >= bmpW || relY >= bmpH) return null;

        var px = (int)Math.Floor(relX / Scale);
        var py = (int)Math.Floor(relY / Scale);
        
        return new PixelPoint(px, py);
    }
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        CurrentPixelCoord = GetPixelCoord(e);

        if (CurrentPixelCoord == HoverPixel) return;

        HoverPixel = CurrentPixelCoord;

        if (!_isLeftPressed) return;
        switch (SelectedTool)
        {
            case ToolType.Pen:
                Paint();
                break;
            case ToolType.Eraser:
                Erase();
                break;
            case ToolType.ColorPicker:
            case ToolType.Fill:
            case ToolType.Hand:
            case ToolType.None: break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), SelectedTool, "Invalid tool type in OnPointerMoved.");
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _isLeftPressed = true;

        switch (SelectedTool)
        {
            case ToolType.Pen:
                Paint();
                break;
            case ToolType.ColorPicker:
                PickColor();
                break;
            case ToolType.Fill:
                Fill();
                break;
            case ToolType.Eraser:
                Erase();
                break;
            case ToolType.Hand: 
            case ToolType.None: break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), SelectedTool, "Invalid tool type in OnPointerPressed.");
        }
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        CurrentPixelCoord = null;
        HoverPixel = null;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        if (e.InitialPressMouseButton == MouseButton.Left) _isLeftPressed = false;
    }

    private void Paint()
    {
        if (HoverPixel is null) return;
        UpdatePixelData(HoverPixel.Value.X, HoverPixel.Value.Y, PickedColor);
    }

    private void PickColor()
    {
        if (_pixelData is null || HoverPixel is null) return;
        PickedColor = BitmapService.GetPixelColor(_pixelData, BitmapWidth, HoverPixel.Value);
    }

    private void Fill()
    {
        if (_pixelData is null || HoverPixel is null || CurrentPixelCoord is null) return;

        BitmapService.FillSimilarPixels(_renderBitmap, _pixelData, BitmapWidth, BitmapHeight, CurrentPixelCoord.Value, PickedColor);
    }

    private void Erase()
    {
        if (HoverPixel is null) return;
        UpdatePixelData(HoverPixel.Value.X, HoverPixel.Value.Y, Colors.Transparent);
    }

    private static Color GetHighlightColor(Color color)
    {
        var factor = color.A == 0 ? 0 : ((color.R + color.G + color.B) / 3 <= 127 ? 255 : 0);

        var highlightColor = new Color(
            51,
            (byte)factor,
            (byte)factor,
            (byte)factor);

        return highlightColor;
    }

    private void UpdatePixelData(int x, int y, Color color)
    {
        if (_pixelData is null) return;
        var stride = BitmapWidth * 4;
        var index = y * stride + x * 4;

        if (color.A == 0)
        {
            _pixelData[index + 0] = 0;
            _pixelData[index + 1] = 0;
            _pixelData[index + 2] = 0;
            _pixelData[index + 3] = 0;
        }
        else
        {
            _pixelData[index + 0] = color.B;
            _pixelData[index + 1] = color.G;
            _pixelData[index + 2] = color.R;
            _pixelData[index + 3] = color.A;
        }

        _renderBitmapDirty = true;
        _previewDirty = true;
    }

    private static void DownscaleNearest(byte[] src, int srcW, int srcH, byte[] dst, int dstW, int dstH, CancellationToken token)
    {
        for (int y = 0; y < dstH; y++)
        {
            if ((y & 7) == 0 && token.IsCancellationRequested) return;

            var srcY = (int)((long)y * srcH / dstH);
            if (srcY >= srcH) srcY = srcH - 1;

            var dstRow = y * dstW * 4;
            var srcRow = srcY * srcW * 4;

            for (int x = 0; x < dstW; x++)
            {
                var srcX = (int)((long)x * srcW / dstW);
                if (srcX >= srcW) srcX = srcW - 1;
                Buffer.BlockCopy(src, srcRow + srcX * 4, dst, dstRow + x * 4, 4);
            }
        }
    }

    private void RequestPreview(short width, short height)
    {
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        var thisCts = _previewCts;

        var size = width * height * 4;

        Task.Run(() =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                DownscaleNearest(_pixelData!, BitmapWidth, BitmapHeight, buffer, width, height, token);

                if (token.IsCancellationRequested) return;

                Dispatcher.UIThread.Post(() =>
                {
                    if (ReferenceEquals(_previewCts, thisCts))
                    {
                        var old = _previewBitmap;
                        _previewBitmap = BitmapService.CreateBitmap(width, height, buffer);

                        Dispatcher.UIThread.Post(() => old?.Dispose(), DispatcherPriority.Background);

                        InvalidateVisual();
                    }

                    ArrayPool<byte>.Shared.Return(buffer);
                }, DispatcherPriority.Normal);
            }
            catch (Exception)
            {
                try { ArrayPool<byte>.Shared.Return(buffer); } catch { }
            }
        }, CancellationToken.None);
    }

    private void EnsurePreviewBitmap(double bmpW, double bmpH)
    {
        if (_renderBitmap is null || _pixelData is null) return;

        const int minPreviewSize = 128;
        var targetW = (int)Math.Ceiling(bmpW);
        var targetH = (int)Math.Ceiling(bmpH);

        if (!_previewDirty && _previewBitmap != null && _previewBitmap.PixelSize.Width == targetW && _previewBitmap.PixelSize.Height == targetH) return;

        if (targetW >= BitmapWidth && targetH >= BitmapHeight)
        {
            _previewBitmap = null;
            _previewDirty = false;
            return;
        }

        RequestPreview((short)Math.Max(minPreviewSize, targetW), (short)Math.Max(minPreviewSize, targetH));
        _previewDirty = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_renderBitmap is null || _pixelData is null) return;

        if (_renderBitmapDirty)
        {
            BitmapService.SetPixelData(_renderBitmap, _pixelData);
            _renderBitmapDirty = false;
            _previewDirty = true;
        }

        var ((bmpW, bmpH), (offsetX, offsetY)) = GetBitmapRenderInfo();

        if (bmpW <= 0 || bmpH <= 0) return;

        EnsurePreviewBitmap(bmpW, bmpH);

        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(8, 8, BitmapService.CreateZeroPixelData(8, 8)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 81, 81, RelativeUnit.Absolute)
        };

        _checkerboardBrush.Transform = new TranslateTransform(offsetX % 81, offsetY % 81);
        context.FillRectangle(_checkerboardBrush, new Rect(offsetX, offsetY, bmpW, bmpH));

        if (_previewBitmap is not null)
        {
            context.DrawImage(
                _previewBitmap,
                new Rect(0, 0, _previewBitmap.PixelSize.Width, _previewBitmap.PixelSize.Height),
                new Rect(offsetX, offsetY, bmpW, bmpH)
            );
        }
        else
        {
            context.DrawImage(
                _renderBitmap,
                new Rect(0, 0, _renderBitmap.PixelSize.Width, _renderBitmap.PixelSize.Height),
                new Rect(offsetX, offsetY, bmpW, bmpH)
            );
        }

        if (HoverPixel is not null)
        {
            var rect = new Rect(
                offsetX + HoverPixel.Value.X * Scale,
                offsetY + HoverPixel.Value.Y * Scale,
                Scale, Scale);
            
            var color = GetHighlightColor(BitmapService.GetPixelColor(_pixelData, BitmapWidth, HoverPixel.Value));
            context.DrawRectangle(new SolidColorBrush(color), null, rect);
        }

        if (_settings.EnableGrid)
        {
            var srcW = _renderBitmap.PixelSize.Width;
            var srcH = _renderBitmap.PixelSize.Height;

            var startX = Math.Max(0, (int)Math.Floor((0 - offsetX) / Scale));
            var endX = Math.Min(srcW, (int)Math.Ceiling((Bounds.Width - offsetX) / Scale));
            var startY = Math.Max(0, (int)Math.Floor((0 - offsetY) / Scale));
            var endY = Math.Min(srcH, (int)Math.Ceiling((Bounds.Height - offsetY) / Scale));

            if (!(Bounds.Width / Scale > _settings.GridMaxSize || Bounds.Height / Scale > _settings.GridMaxSize))
            {
                for (var x = startX; x <= endX; x++)
                {
                    var xPos = offsetX + x * Scale;
                    context.DrawLine(_gridPen, new Point(xPos, offsetY), new Point(xPos, offsetY + bmpH));
                }

                for (var y = startY; y <= endY; y++)
                {
                    var yPos = offsetY + y * Scale;
                    context.DrawLine(_gridPen, new Point(offsetX, yPos), new Point(offsetX + bmpW, yPos));
                }
            }
        }
    }
}