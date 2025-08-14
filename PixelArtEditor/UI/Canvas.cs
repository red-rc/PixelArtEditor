using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.Other;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.UI;

public class Canvas : Control
{
    private static readonly Settings Settings = Settings.GetInstance;
    
    public static readonly StyledProperty<Vector2> OffsetProperty =
        AvaloniaProperty.Register<Canvas, Vector2>(nameof(Offset), new Vector2(0, 0));

    public Vector2 Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }
    
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<Canvas, double>(nameof(Scale), 1.0);

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

    private bool _isPressed;

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
    
    private readonly Pen _gridPen = new(new SolidColorBrush(Color.Parse(Settings.GridColor)));

    private static readonly double GridMaxSize = Settings.GridMaxSize;

    private WriteableBitmap? _zeroBitmap;
    
    private WriteableBitmap? _bitmap;
    private byte[]? _pixelData;
    
    public Canvas()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        
        this.GetObservable(ParametersProperty)
            .Subscribe(param =>
            {
                if (param == null) return;
        
                _pixelData = BitmapService.CreatePixelData(param.Width, param.Height, param.BackgroundColor);
                _bitmap = BitmapService.CreateBitmap(param.Width, param.Height, _pixelData);
                
                _zeroBitmap = BitmapService.CreateBitmap(
                    param.Width,
                    param.Height,
                    BitmapService.CreateZeroPixelData(param.Width, param.Height));
                InvalidateVisual();
            });
        
        this.GetObservable(OffsetProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(ScaleProperty).Subscribe(_ =>
        {
            if (Parameters != null)
            {
                _zeroBitmap = BitmapService.CreateBitmap(
                    Parameters.Width,
                    Parameters.Height,
                    BitmapService.CreateZeroPixelData(Parameters.Width, Parameters.Height));
            }
            
            InvalidateVisual();
        });
    }
    
    private PixelPoint? GetPixelCoord(PointerEventArgs e)
    {
        if (Parameters is null || _bitmap is null) return null;

        var pos = e.GetPosition(this);

        var bmpPx = _bitmap.PixelSize;

        var bmpW = bmpPx.Width * Scale;
        var bmpH = bmpPx.Height * Scale;

        var offsetX = (Bounds.Width - bmpW) / 2 + Offset.X;
        var offsetY = (Bounds.Height - bmpH) / 2 + Offset.Y;

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

        if (!_isPressed) return;
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
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        _isPressed = true;

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
                throw new ArgumentOutOfRangeException();
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
        
        _isPressed = false;
    }

    private void Paint()
    {
        if (HoverPixel is null) return;
        UpdatePixelData(HoverPixel.Value.X, HoverPixel.Value.Y, PickedColor);
    }

    private void PickColor()
    {
        if (_pixelData is null || Parameters is null || HoverPixel is null) return;
        PickedColor = BitmapService.GetPixelColor(_pixelData, Parameters.Width, HoverPixel.Value);
    }

    private void Fill(PointerPressedEventArgs e)
    {
        var pixelCoord = GetPixelCoord(e);
        
        if (Parameters is null || pixelCoord is null || _pixelData is null) return;
        var similarPixels = BitmapService.GetSimilarPixels(_pixelData, Parameters.Width, Parameters.Height, pixelCoord.Value);

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
        if (Parameters is null || _bitmap is null || _pixelData is null) return;
        var stride = Parameters.Width * 4;
        var index = y * stride + x * 4;
        
        _pixelData[index + 0] = color.B;
        _pixelData[index + 1] = color.G;
        _pixelData[index + 2] = color.R;
        _pixelData[index + 3] = color.A;

        BitmapService.UpdateBitmap(_bitmap, _pixelData);
        InvalidateVisual(); 
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_zeroBitmap is null || _bitmap is null || _pixelData is null || Parameters is null) return;
        
        var bmpPx = _bitmap.PixelSize;
        var bmpW = bmpPx.Width * Scale;
        var bmpH = bmpPx.Height * Scale;
        
        var offsetX = (Bounds.Width - bmpW) / 2 + Offset.X;
        var offsetY = (Bounds.Height - bmpH) / 2 + Offset.Y;

        var destRect = new Rect(offsetX, offsetY, bmpW, bmpH);
        var srcRect = new Rect(0, 0, bmpPx.Width, bmpPx.Height);

        context.DrawImage(_zeroBitmap, srcRect, destRect);
        context.DrawImage(_bitmap, srcRect, destRect);

        if (HoverPixel is not null)
        {
            var pixelColor = BitmapService.GetPixelColor(_pixelData, Parameters.Width, HoverPixel.Value);

            if (pixelColor.A != 0)
            {
                var rect = new Rect(
                    offsetX + HoverPixel.Value.X * Scale,
                    offsetY + HoverPixel.Value.Y * Scale,
                    Scale, Scale);

                var color = GetHighlightColor(BitmapService.GetPixelColor(_pixelData, Parameters.Width,
                    HoverPixel.Value));
                context.DrawRectangle(new SolidColorBrush(color), null, rect);
            }
        }

        if (Bounds.Width / Scale > GridMaxSize || Bounds.Height / Scale > GridMaxSize) return;
        
        if (!Settings.EnableGrid) return;
        
        for (var x = 0; x <= bmpPx.Width; x++)
        {
            var xPos = offsetX + x * Scale;
            context.DrawLine(_gridPen, new Point(xPos, offsetY), new Point(xPos, offsetY + bmpH));
        }
        
        for (var y = 0; y <= bmpPx.Height; y++)
        {
            var yPos = offsetY + y * Scale;
            context.DrawLine(_gridPen, new Point(offsetX, yPos), new Point(offsetX + bmpW, yPos));
        }
    }
}