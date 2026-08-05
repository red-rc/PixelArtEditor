using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Tools;
using PixelArtEditor.AppServices.Tools.Implementations;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace PixelArtEditor.Controls.Editor;

public class Canvas : Control, ICanvasContext
{
    private static readonly ISettingsManager _settings = Services.Settings;
    private readonly Pen _gridPen = new(new SolidColorBrush(_settings.GridColor));
    private bool _isLeftPressed;

    public static readonly StyledProperty<PixelModel> ModelProperty =
        AvaloniaProperty.Register<Canvas, PixelModel>(nameof(Model));

    public PixelModel Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private PixelModel? _subscribedModel;

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
        set
        {
            SetValue(SelectedToolProperty, value);
        }
    }

    private ITool _currentTool = new EmptyTool();

    private PixelPoint? _hoverPixel;
    public PixelPoint? HoverPixel
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

    public static readonly StyledProperty<PixelPoint?> CurrentPixelCoordProperty =
    AvaloniaProperty.Register<Canvas, PixelPoint?>(nameof(CurrentPixelCoord));

    public PixelPoint? CurrentPixelCoord
    {
        get => GetValue(CurrentPixelCoordProperty);
        set => SetValue(CurrentPixelCoordProperty, value);
    }

    private ImageBrush? _checkerboardBrush;

    public LayerManager LayerManager { get; set; }
    public Dictionary<LayerModel, LayerRenderCache> RenderCache { get; } = [];

    public Canvas()
    {
        LayerManager = new LayerManager();

        LayerManager.Layers.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                InvalidateVisual();
                return;
            }

            if (e.NewItems is not null)
                foreach (LayerModel layer in e.NewItems)
                {
                    RenderCache[layer] = new LayerRenderCache();
                    layer.PropertyChanged += Layer_PropertyChanged;
                }

            if (e.OldItems is not null)
                foreach (LayerModel layer in e.OldItems)
                {
                    RenderCache[layer].PreviewCts?.Cancel();
                    RenderCache.Remove(layer);

                    layer.PropertyChanged -= Layer_PropertyChanged;
                }

            InvalidateVisual();
        };

        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

        ModelProperty.Changed.AddClassHandler<Canvas>((sender, e) =>
        {
            _subscribedModel?.ModelChanged -= OnModelChanged;

            _subscribedModel = sender.Model;

            _subscribedModel?.ModelChanged += OnModelChanged;

            sender.OnModelChanged();
        });

        this.GetObservable(ModelProperty).Subscribe(_ => OnModelChanged());
        this.GetObservable(OffsetProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(ScaleProperty).Subscribe(_ => InvalidateVisual());
    }

    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var layer = (LayerModel)sender!;

        if (e.PropertyName == nameof(LayerModel.Opacity))
        {
            RenderCache[layer].RenderBitmapDirty = true;
            RenderCache[layer].PreviewDirty = true;
        }

        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedToolProperty)
            _currentTool = ToolManager.Get((ToolType)change.NewValue!);
    }

    private void OnModelChanged()
    {
        if (Model is null || LayerManager.Layers.Count == 0) return;
        LayerManager.ResizeLayers(Model.Width, Model.Height);
        InvalidateVisual();
    }

    public void ResetLayerManager()
    {
        LayerManager.Layers.Clear();
        RenderCache.Clear();
        LayerManager.ActiveLayer = null;
    }

    public void InitializeWithModel(PixelModel model)
    {
        LayerManager.Layers.Add(new LayerModel(
            model.Width,
            model.Height,
            BitmapService.SwapRB(model.Data),
            "Layer 1"));

        var layer = LayerManager.Layers[0];
        LayerManager.ActiveLayer = layer;
        RenderCache[layer].RenderBitmapDirty = false;
        RenderCache[layer].PreviewDirty = true;

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        CurrentPixelCoord = CanvasHelper.GetPixelCoord(this, this, e);
        if (CurrentPixelCoord == HoverPixel) return;
        HoverPixel = CurrentPixelCoord;

        if (!_isLeftPressed) return;
        if (LayerManager.ActiveLayer is { IsVisible: false } or { IsLocked: true }) return;

        _currentTool.OnPointerMoved(this);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        if (LayerManager.ActiveLayer is { IsVisible: false } or { IsLocked: true }) return;

        _isLeftPressed = true;
        _currentTool.OnPointerPressed(this);
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

    private void DrawCheckerBoard(DrawingContext context, double offsetX, double offsetY, double bmpW, double bmpH)
    {
        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(8, 8, BitmapService.CreateCheckerBoardPixelData(8, 8)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 81, 81, RelativeUnit.Absolute)
        };

        _checkerboardBrush.Transform = new TranslateTransform(offsetX % 81, offsetY % 81);
        context.FillRectangle(_checkerboardBrush, new Rect(offsetX, offsetY, bmpW, bmpH));
    }

    private static void DrawBitmap(DrawingContext context, LayerModel layer, double offsetX, double offsetY, double bmpW, double bmpH)
    {
        if (layer.RenderBitmap is null) return;

        if (layer.PreviewBitmap is not null)
        {
            context.DrawImage(
                layer.PreviewBitmap,
                new Rect(0, 0, layer.Width, layer.Height),
                new Rect(offsetX, offsetY, bmpW, bmpH)
            );
        }
        else
        {
            context.DrawImage(
                layer.RenderBitmap,
                new Rect(0, 0, layer.Width, layer.Height),
                new Rect(offsetX, offsetY, bmpW, bmpH)
            );
        }
    }

    private void DrawHoverPixel(DrawingContext context, double offsetX, double offsetY)
    {
        if (HoverPixel is null) return;

        var rect = new Rect(
            offsetX + HoverPixel.Value.X * Scale,
            offsetY + HoverPixel.Value.Y * Scale,
            Scale, Scale);

        var color = CanvasHelper.GetHighlightColor(BitmapService.GetPixelColor(
            LayerManager.GetCompositePixelData(Model.Width, Model.Height), 
            Model.Width, 
            HoverPixel.Value));
        context.DrawRectangle(new SolidColorBrush(color), null, rect);
    }

    private void DrawGrid(DrawingContext context, double offsetX, double offsetY, double bmpW, double bmpH)
    {
        if (!_settings.EnableGrid) return;

        var startX = Math.Max(0, (int)Math.Floor((0 - offsetX) / Scale));
        var endX = Math.Min(Model.Width, (int)Math.Ceiling((Bounds.Width - offsetX) / Scale));
        var startY = Math.Max(0, (int)Math.Floor((0 - offsetY) / Scale));
        var endY = Math.Min(Model.Height, (int)Math.Ceiling((Bounds.Height - offsetY) / Scale));

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

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var ((bmpW, bmpH), (offsetX, offsetY)) = CanvasHelper.GetBitmapRenderInfo(this);
        if (bmpW <= 0 || bmpH <= 0) return;

        foreach (var layer in LayerManager.Layers.Reverse())
        {
            if (!RenderCache.TryGetValue(layer, out var cache)) continue;

            if (cache.RenderBitmapDirty)
            {
                var pixelData = layer.PixelData.ToArray();

                for (var i = 0; i < pixelData.Length; i += 4)
                    pixelData[i + 3] = (byte)(pixelData[i + 3] * layer.Opacity);

                BitmapService.SetPixelData(layer.RenderBitmap, pixelData);
                cache.RenderBitmapDirty = false;
                cache.PreviewDirty = true;
            }

            PreviewService.EnsurePreviewBitmap(this, layer, InvalidateVisual, bmpW, bmpH);
        }

        DrawCheckerBoard(context, offsetX, offsetY, bmpW, bmpH);

        foreach (var layer in LayerManager.Layers.Reverse())
            if (layer.IsVisible)
                DrawBitmap(context, layer, offsetX, offsetY, bmpW, bmpH);

        DrawHoverPixel(context, offsetX, offsetY);
        DrawGrid(context, offsetX, offsetY, bmpW, bmpH);
    }
}