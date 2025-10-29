using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using PixelArtEditor.ViewModels;
using System;
using System.Numerics;

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
    private WriteableBitmap? _scaledBitmap;
    private byte[]? _pixelData;
    private bool _renderBitmapDirty;
    
    public Canvas()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

        this.GetObservable(ParametersProperty).Subscribe(param =>
        {
            if (param is null) return;
        
            _pixelData = Services.Bitmap.CreatePixelData(param.Width, param.Height, param.BackgroundColor);

            _renderBitmap?.Dispose();
            _renderBitmap = Services.Bitmap.CreateBitmap(param.Width, param.Height, _pixelData);
            
            InvalidateVisual();
        });
        this.GetObservable(OffsetProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(ScaleProperty).Subscribe(_ =>
        {
            UpdateScaledBitmap();
            InvalidateVisual();
        });
    }
    
    private void UpdateScaledBitmap()
    {
        if (_renderBitmap is null || _pixelData is null || Parameters is null) return;
        
        _scaledBitmap?.Dispose();
        
        if (Scale >= 1)
        {
            _scaledBitmap = null;
            return;
        }
        
        var newWidth = (int)Math.Ceiling(_renderBitmap.PixelSize.Width * Scale);
        var newHeight = (int)Math.Ceiling(_renderBitmap.PixelSize.Height * Scale);
        
        var pixelData =
            Services.Bitmap.CreateScaledPixelData(_pixelData, Parameters.Width, Parameters.Height, newWidth, newHeight);
        
        _scaledBitmap = Services.Bitmap.CreateBitmap(newWidth, newHeight, pixelData);
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
        if (Parameters is null || _renderBitmap is null) return null;

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

        var pixelCoord = GetPixelCoord(e);

        if (pixelCoord == HoverPixel) return;

        HoverPixel = pixelCoord;

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
                Fill(e);
                break;
            case ToolType.Eraser:
                Erase();
                break;
            case ToolType.None: break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), SelectedTool, "Invalid tool type in OnPointerPressed.");
        }
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
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
        if (_pixelData is null || Parameters is null || HoverPixel is null) return;
        PickedColor = Services.Bitmap.GetPixelColor(_pixelData, Parameters.Width, HoverPixel.Value);
    }

    private void Fill(PointerPressedEventArgs e)
    {
        var pixelCoord = GetPixelCoord(e);
        
        if (Parameters is null || pixelCoord is null || _pixelData is null) return;
        var similarPixels = Services.Bitmap.GetSimilarPixels(_pixelData, Parameters.Width, Parameters.Height, pixelCoord.Value);

        if (similarPixels is null || similarPixels.Count < 1) return;
        foreach (var pixel in similarPixels)
        {
            UpdatePixelData(pixel.X, pixel.Y, PickedColor);
        }
    }

    private void Erase()
    {
        if (HoverPixel is null) return;
        UpdatePixelData(HoverPixel.Value.X, HoverPixel.Value.Y, Colors.Transparent);
    }

    private static Color GetHighlightColor(Color color)
    {
        var factor = (color.R + color.G + color.B) / 3 <= 127 ? 255 : 0;

        var highlightColor = new Color(
            51,
            (byte)factor,
            (byte)factor,
            (byte)factor);

        return highlightColor;
    }
        
    private void UpdatePixelData(int x, int y, Color color)
    {
        if (Parameters is null || _pixelData is null) return;
        var stride = Parameters.Width * 4;
        var index = y * stride + x * 4;
        
        _pixelData[index + 0] = color.B;
        _pixelData[index + 1] = color.G;
        _pixelData[index + 2] = color.R;
        _pixelData[index + 3] = color.A;
        
        _renderBitmapDirty = true;
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (_renderBitmap is null || _pixelData is null || Parameters is null) return;
        
        if (_renderBitmapDirty)
        {
            Services.Bitmap.UpdateBitmap(_renderBitmap, _pixelData);
            _renderBitmapDirty = false;
        }

        var ((bmpW, bmpH), (offsetX, offsetY)) = GetBitmapRenderInfo();
        
        _checkerboardBrush ??= new ImageBrush(
            Services.Bitmap.CreateBitmap(8, 8, Services.Bitmap.CreateZeroPixelData(8, 8)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 81, 81, RelativeUnit.Absolute)
        };

        _checkerboardBrush.Transform = new TranslateTransform(offsetX % 81, offsetY % 81);
        context.FillRectangle(_checkerboardBrush, new Rect(offsetX, offsetY, bmpW, bmpH));
        
        var bitmapToDraw = _scaledBitmap != null && Scale < 1 ? _scaledBitmap : _renderBitmap;

        context.DrawImage(
            bitmapToDraw,
            new Rect(0, 0, bitmapToDraw.PixelSize.Width, bitmapToDraw.PixelSize.Height),
            new Rect(offsetX, offsetY, bmpW, bmpH)
        );

        if (HoverPixel is not null)
        {
            var pixelColor = Services.Bitmap.GetPixelColor(_pixelData, Parameters.Width, HoverPixel.Value);

            if (pixelColor.A != 0)
            {
                var rect = new Rect(
                    offsetX + HoverPixel.Value.X * Scale,
                    offsetY + HoverPixel.Value.Y * Scale,
                    Scale, Scale);

                var color = GetHighlightColor(Services.Bitmap.GetPixelColor(_pixelData, Parameters.Width,
                    HoverPixel.Value));
                context.DrawRectangle(new SolidColorBrush(color), null, rect);
            }
        }

        if (Bounds.Width / Scale > _settings.GridMaxSize
            || Bounds.Height / Scale > _settings.GridMaxSize
            || !_settings.EnableGrid) return;
        
        for (var x = 0; x <= _renderBitmap.PixelSize.Width; x++)
        {
            var xPos = offsetX + x * Scale;
            context.DrawLine(_gridPen, new Point(xPos, offsetY), new Point(xPos, offsetY + bmpH));
        }
        
        for (var y = 0; y <= _renderBitmap.PixelSize.Height; y++)
        {
            var yPos = offsetY + y * Scale;
            context.DrawLine(_gridPen, new Point(offsetX, yPos), new Point(offsetX + bmpW, yPos));
        }
    }
}