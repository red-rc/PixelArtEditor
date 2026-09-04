using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using System;
using System.Numerics;

namespace PixelArtEditor.UI;

public class Checkerboard : Control
{
    private static ISettingsManager Settings => Services.Settings;

    public static readonly StyledProperty<PixelModel> ModelProperty =
        AvaloniaProperty.Register<Checkerboard, PixelModel>(nameof(Model));

    public PixelModel Model 
    { 
        get => GetValue(ModelProperty); 
        set => SetValue(ModelProperty, value); 
    }

    private PixelModel? _subscribedModel;

    public static readonly StyledProperty<int> DataWidthProperty =
        AvaloniaProperty.Register<Checkerboard, int>(nameof(DataWidth));

    public int DataWidth 
    { 
        get => GetValue(DataWidthProperty); 
        set => SetValue(DataWidthProperty, value); 
    }

    public static readonly StyledProperty<int> DataHeightProperty =
        AvaloniaProperty.Register<Checkerboard, int>(nameof(DataHeight));

    public int DataHeight 
    { 
        get => GetValue(DataHeightProperty); 
        set => SetValue(DataHeightProperty, value); 
    }

    public static readonly StyledProperty<double?> FixedTileSizeProperty =
        AvaloniaProperty.Register<Checkerboard, double?>(nameof(FixedTileSize));

    public double? FixedTileSize
    {
        get => GetValue(FixedTileSizeProperty);
        set => SetValue(FixedTileSizeProperty, value);
    }

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
        set
        {
            _prevScale = Scale;
            SetValue(ScaleProperty, value);
        }
    }

    private double _prevScale;
    private CheckerboardScale _prevCheckerboardScale;

    public static readonly StyledProperty<int> MaxScaleProperty =
        AvaloniaProperty.Register<Canvas, int>(nameof(MaxScale));

    public int MaxScale
    {
        get => GetValue(MaxScaleProperty);
        set => SetValue(MaxScaleProperty, value);
    }

    private bool _scaleWithCanvas = Settings.ScaleCheckerboardWithCanvas;
    private DrawingBrush? _checkerboardBrush;
    private readonly BoxShadows _shadow = Application.Current?.Resources["CardShadow"] as BoxShadows? ?? default;

    public Checkerboard()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Settings.ScaleCheckerboardWithCanvas) or nameof(Settings.CheckerboardScale))
                InvalidateVisual();
        };

        ModelProperty.Changed.AddClassHandler<Checkerboard>((sender, e) =>
        {
            sender._subscribedModel?.ModelChanged -= sender.OnModelChanged;

            sender._subscribedModel = sender.Model;

            sender._subscribedModel?.ModelChanged += sender.OnModelChanged;

            sender.InvalidateVisual();
        });

        this.GetObservable(OffsetProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(ScaleProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(DataWidthProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(DataHeightProperty).Subscribe(_ => InvalidateVisual());
    }

    private void OnModelChanged() => InvalidateVisual();

    private void DrawCheckerBoard(DrawingContext context, int offsetX, int offsetY, int bmpW, int bmpH)
    {
        _checkerboardBrush ??= CreateBaseBrush();

        if (_prevScale != Scale
            || Settings.ScaleCheckerboardWithCanvas != _scaleWithCanvas
            || Settings.CheckerboardScale != _prevCheckerboardScale)
        {
            var tileSize = Settings.ScaleCheckerboardWithCanvas
                ? Scale * (int)Settings.CheckerboardScale * 2
                : MaxScale / 4;

            while (Settings.ScaleCheckerboardWithCanvas && tileSize < 32)
                tileSize *= 2;

            _checkerboardBrush.DestinationRect = new RelativeRect(0, 0, tileSize, tileSize, RelativeUnit.Absolute);

            _prevScale = Scale;
            _scaleWithCanvas = Settings.ScaleCheckerboardWithCanvas;
            _prevCheckerboardScale = Settings.CheckerboardScale;
        }

        _checkerboardBrush.Transform = new TranslateTransform(offsetX, offsetY);

        context.FillRectangle(_checkerboardBrush, new Rect(offsetX, offsetY, bmpW, bmpH));
    }

    private void DrawFixedCheckerBoard(DrawingContext context, double tileSize, Rect rect)
    {
        _checkerboardBrush ??= CreateBaseBrush();
        _checkerboardBrush.DestinationRect = new RelativeRect(0, 0, tileSize, tileSize, RelativeUnit.Absolute);

        context.FillRectangle(_checkerboardBrush, rect);
    }

    private static DrawingBrush CreateBaseBrush()
    {
        var imageDrawing = new ImageDrawing
        {
            ImageSource = BitmapService.CreateBitmap(2, 2, BitmapService.CreateCheckerBoardPixelData(2, 2)),
            Rect = new Rect(0, 0, 2, 2)
        };

        return new DrawingBrush(imageDrawing)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
        };
    }

    public override void Render(DrawingContext context)
    {
        if (FixedTileSize is double fixedSize)
        {
            if (Bounds.Width != Bounds.Height)
                throw new InvalidOperationException(LocalizationService.Get("PreviewSquareError"));

            var rect = PreviewHelper.GetAspectRect(Bounds.Width, DataWidth, DataHeight);

            context.DrawRectangle(null, null, rect, 0, 0, _shadow);
            DrawFixedCheckerBoard(context, fixedSize, rect);
        }

        else
        {
            var (bmpW, bmpH, offsetX, offsetY) = CanvasHelper.GetBitmapRenderInfo(Scale, Offset, Bounds, Model);
            if (bmpW <= 0 || bmpH <= 0) return;

            context.DrawRectangle(null, null, new Rect(offsetX, offsetY, bmpW, bmpH), 0, 0, _shadow);
            DrawCheckerBoard(context, offsetX, offsetY, bmpW, bmpH);
        }
    }
}
