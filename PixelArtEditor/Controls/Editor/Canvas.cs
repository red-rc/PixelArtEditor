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
using PixelArtEditor.ViewModels;
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

    public static readonly StyledProperty<int> MaxScaleProperty =
        AvaloniaProperty.Register<Canvas, int>(nameof(MaxScale));

    public int MaxScale
    {
        get => GetValue(MaxScaleProperty);
        set => SetValue(MaxScaleProperty, value);
    }

    public static readonly StyledProperty<bool> IsDownscaledProperty =
        AvaloniaProperty.Register<Canvas, bool>(nameof(IsDownscaled));

    public bool IsDownscaled
    {
        get => GetValue(IsDownscaledProperty);
        set => SetValue(IsDownscaledProperty, value);
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

    public static bool CanEdit => Services.Navigation.GetViewModel() is EditorVM editorVM && !editorVM.IsTransforming;

    private PixelPoint? _hoverPixel;
    public PixelPoint? HoverPixel
    {
        get => _hoverPixel;
        set
        {
            if (_hoverPixel == value) return;
            _hoverPixel = value;
            HoverPixelColor = null;
            InvalidateVisual();
        }
    }

    private Color? _hoverPixelColor;
    public Color? HoverPixelColor
    {
        get => _hoverPixelColor;
        set
        {
            if (_hoverPixelColor == value) return;
            _hoverPixelColor = value;
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

    public LayerManager LayerManager { get; private set; } = null!;
    public Dictionary<LayerModel, LayerRenderCache> RenderCache { get; } = [];

    public Canvas()
    {
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

        if (e.PropertyName is nameof(LayerModel.PixelData) or nameof(LayerModel.Opacity))
            HoverPixelColor = null;

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

    public void AttachLayerManager(LayerManager layerManager)
    {
        LayerManager?.Layers.CollectionChanged -= OnLayersChanged;

        RenderCache.Clear();

        LayerManager = layerManager;
        LayerManager.Layers.CollectionChanged += OnLayersChanged;

        foreach (var layer in LayerManager.Layers)
        {
            RenderCache[layer] = new LayerRenderCache
            {
                RenderBitmapDirty = false,
                PreviewDirty = true
            };
            layer.PropertyChanged += Layer_PropertyChanged;
        }

        InvalidateVisual();
    }

    public void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        CurrentPixelCoord = CanvasHelper.GetPixelCoord(this, this, e);
        if (CurrentPixelCoord == HoverPixel) return;
        HoverPixel = CurrentPixelCoord;

        if (!_isLeftPressed) return;
        if (LayerManager.ActiveLayer is { IsVisible: false } or { IsLocked: true } || !CanEdit) return;

        _currentTool.OnPointerMoved(this);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        if (LayerManager.ActiveLayer is { IsVisible: false } or { IsLocked: true } || !CanEdit) return;

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

    private void DrawCheckerBoard(DrawingContext context, int offsetX, int offsetY, int bmpW, int bmpH)
    {
        var tileSize = _settings.ScaleCheckerboardWithCanvas
            ? Scale * (int)_settings.CheckerboardScale * 2
            : MaxScale / 4;

        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(2, 2, BitmapService.CreateCheckerBoardPixelData(2, 2)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill
        };

        _checkerboardBrush.DestinationRect = new RelativeRect(0, 0, tileSize, tileSize, RelativeUnit.Absolute);
        _checkerboardBrush.Transform = new TranslateTransform(offsetX, offsetY);
        context.FillRectangle(_checkerboardBrush, new Rect(offsetX, offsetY, bmpW, bmpH));
    }

    private void DrawBitmap(DrawingContext context, LayerModel layer, double offsetX, double offsetY)
    {
        if (layer.RenderBitmap is null) return;

        var srcRect = new Rect(0, 0, Model.Width, Model.Height);
        var destRect = new Rect(offsetX, offsetY, Model.Width * Scale, Model.Height * Scale);

        if (IsDownscaled && layer.PreviewBitmap is not null)
        {
            var scaleX = (double)layer.PreviewBitmap.PixelSize.Width / layer.Width;
            var scaleY = (double)layer.PreviewBitmap.PixelSize.Height / layer.Height;

            using (context.PushOpacity(layer.Opacity))
            {
                context.DrawImage(layer.PreviewBitmap, new Rect(0, 0, Model.Width * scaleX, Model.Height * scaleY), destRect);
            }
        }
        else
        {
            using (context.PushOpacity(layer.Opacity))
            {
                context.DrawImage(layer.RenderBitmap, srcRect, destRect);
            }
        }
    }

    private void DrawHoverPixel(DrawingContext context, double offsetX, double offsetY)
    {
        if (HoverPixel is null) return;

        var rect = new Rect(
            offsetX + HoverPixel.Value.X * Scale,
            offsetY + HoverPixel.Value.Y * Scale,
            Scale, Scale);

        HoverPixelColor ??= CanvasHelper.GetHighlightColor(BitmapService.GetCompositePixelColor(LayerManager.Layers, HoverPixel.Value));

        if (HoverPixelColor is Color color)
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

        var (bmpW, bmpH, offsetX, offsetY) = CanvasHelper.GetBitmapRenderInfo(this);
        if (bmpW <= 0 || bmpH <= 0) return;

        foreach (var layer in LayerManager.Layers.Reverse())
        {
            if (!RenderCache.TryGetValue(layer, out var cache)) continue;

            if (cache.RenderBitmapDirty)
            {
                BitmapService.SetPixelData(layer.RenderBitmap, layer.PixelData);
                cache.RenderBitmapDirty = false;
                cache.PreviewDirty = true;
            }

            PreviewService.EnsurePreviewBitmap(this, layer, InvalidateVisual, bmpW, bmpH);
        }

        DrawCheckerBoard(context, offsetX, offsetY, bmpW, bmpH);

        foreach (var layer in LayerManager.Layers.Reverse())
            if (layer.IsVisible)
                DrawBitmap(context, layer, offsetX, offsetY);

        if (!IsDownscaled)
        {
            DrawHoverPixel(context, offsetX, offsetY);
            DrawGrid(context, offsetX, offsetY, bmpW, bmpH);
        }
    }
}