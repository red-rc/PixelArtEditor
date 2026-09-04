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

namespace PixelArtEditor.UI;

public class Canvas : Control, ICanvasContext
{
    private static ISettingsManager Settings => Services.Settings;
    private readonly Pen _gridPen = new(new SolidColorBrush(Settings.GridColor));

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

    public static readonly StyledProperty<ToolType> SelectedToolProperty =
        AvaloniaProperty.Register<Canvas, ToolType>(nameof(SelectedTool));
    
    public ToolType SelectedTool
    {
        get => GetValue(SelectedToolProperty);
        set => SetValue(SelectedToolProperty, value);
    }

    private ITool _currentTool = new EmptyTool();

    public static bool CanEdit => Services.Navigation.GetViewModel() is EditorVM editorVM && !editorVM.IsTransforming;

    public static readonly StyledProperty<PixelPoint?> HoverPixelProperty =
        AvaloniaProperty.Register<Canvas, PixelPoint?>(nameof(HoverPixel));

    public PixelPoint? HoverPixel
    {
        get => GetValue(HoverPixelProperty);
        set
        {
            if (HoverPixel == value) return;
            _hoverPixelColor = null;
            SetValue(HoverPixelProperty, value);
            InvalidateVisual();
        }
    }

    private Color? _hoverPixelColor;

    public static readonly StyledProperty<Color> PickedColorProperty =
        AvaloniaProperty.Register<Canvas, Color>(nameof(PickedColor));
    
    public Color PickedColor
    {
        get => GetValue(PickedColorProperty);
        set => SetValue(PickedColorProperty, value);
    }

    public LayerManager LayerManager { get; private set; } = null!;
    public Dictionary<LayerModel, LayerRenderCache> RenderCache { get; } = [];

    public Canvas()
    {
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Settings.InterpolationMode) or nameof(Settings.InterpolateOnlyWhenScalingDown))
            {
                UpdateInterpolationMode();
                InvalidateVisual();
            }
        };

        ModelProperty.Changed.AddClassHandler<Canvas>((sender, e) =>
        {
            _subscribedModel?.ModelChanged -= OnModelChanged;

            _subscribedModel = sender.Model;

            _subscribedModel?.ModelChanged += OnModelChanged;

            sender.OnModelChanged();
        });

        this.GetObservable(ModelProperty).Subscribe(_ => OnModelChanged());
        this.GetObservable(OffsetProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(ScaleProperty).Subscribe(_ =>
        {
            UpdateInterpolationMode();
            InvalidateVisual();
        });
    }

    private void UpdateInterpolationMode()
    {
        var mode = Settings.InterpolateOnlyWhenScalingDown
            ? (Scale < 1 ? Settings.InterpolationMode : BitmapInterpolationMode.None)
            : Settings.InterpolationMode;

        if (RenderOptions.GetBitmapInterpolationMode(this) != mode)
            RenderOptions.SetBitmapInterpolationMode(this, mode);
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
                PreviewDirty = true,
                RenderRect = layer.IsEmpty ? null : new Rect(0, 0, layer.Width, layer.Height)
            };

            layer.PropertyChanged += OnLayerPropertyChanged;
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
                RenderCache[layer] = new LayerRenderCache()
                {
                    RenderRect = layer.IsEmpty ? null : new Rect(0, 0, layer.Width, layer.Height)
                };

                layer.PropertyChanged += OnLayerPropertyChanged;
            }

        if (e.OldItems is not null)
            foreach (LayerModel layer in e.OldItems)
            {
                RenderCache[layer].PreviewCts?.Cancel();
                RenderCache.Remove(layer);

                layer.PropertyChanged -= OnLayerPropertyChanged;
            }

        InvalidateVisual();
    }

    private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var layer = (LayerModel)sender!;

        if (e.PropertyName == nameof(LayerModel.Opacity))
        {
            RenderCache[layer].RenderBitmapDirty = true;
            RenderCache[layer].PreviewDirty = true;
        }

        if (e.PropertyName is nameof(LayerModel.PixelData) or nameof(LayerModel.Opacity))
            _hoverPixelColor = null;

        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            LayerManager.ActiveLayer is { IsVisible: false } or { IsLocked: true } || !CanEdit) return;

        _currentTool.OnPointerPressed(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        HoverPixel = CanvasHelper.GetPixelCoord(this, this, e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || LayerManager.ActiveLayer is { IsVisible: false } or { IsLocked: true } || !CanEdit) return;

        _currentTool.OnPointerMoved(this);
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        HoverPixel = null;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _currentTool.OnPointerReleased(this);
    }

    private void DrawBitmap(DrawingContext context, LayerModel layer, double offsetX, double offsetY)
    {
        if (layer.RenderBitmap is null || !RenderCache.TryGetValue(layer, out var cache) || cache.RenderRect is null) return;

        var srcRect = new Rect(
            cache.RenderRect.Value.X, 
            cache.RenderRect.Value.Y, 
            cache.RenderRect.Value.Width, 
            cache.RenderRect.Value.Height);

        var dstRect = new Rect(
            offsetX + cache.RenderRect.Value.X * Scale, 
            offsetY + cache.RenderRect.Value.Y * Scale,
            cache.RenderRect.Value.Width * Scale, 
            cache.RenderRect.Value.Height * Scale);

        if (Scale < 1 && layer.PreviewBitmap is not null && cache.PreviewDirty == false)
        {
            var scaleX = (double)layer.PreviewBitmap.PixelSize.Width / layer.Width;
            var scaleY = (double)layer.PreviewBitmap.PixelSize.Height / layer.Height;

            srcRect = new Rect(
                srcRect.X * scaleX,
                srcRect.Y * scaleY,
                srcRect.Width * scaleX,
                srcRect.Height * scaleY);

            using (context.PushOpacity(layer.Opacity))
                context.DrawImage(layer.PreviewBitmap, srcRect, dstRect);
        }
        else
        {
            using (context.PushOpacity(layer.Opacity))
                context.DrawImage(layer.RenderBitmap, srcRect, dstRect);
        }
    }

    private void DrawHoverPixel(DrawingContext context, double offsetX, double offsetY)
    {
        if (HoverPixel is null) return;

        var rect = new Rect(
            offsetX + HoverPixel.Value.X * Scale,
            offsetY + HoverPixel.Value.Y * Scale,
            Scale, Scale);

        _hoverPixelColor ??= CanvasHelper.GetHighlightColor(BitmapService.GetCompositePixelColor(LayerManager.Layers, HoverPixel.Value));

        if (_hoverPixelColor is Color color)
            context.DrawRectangle(new SolidColorBrush(color), null, rect);
    }

    private void DrawGrid(DrawingContext context, double offsetX, double offsetY, double bmpW, double bmpH)
    {
        if (!Settings.EnableGrid) return;

        var startX = Math.Max(0, (int)Math.Floor((0 - offsetX) / Scale));
        var endX = Math.Min(Model.Width, (int)Math.Ceiling((Bounds.Width - offsetX) / Scale));
        var startY = Math.Max(0, (int)Math.Floor((0 - offsetY) / Scale));
        var endY = Math.Min(Model.Height, (int)Math.Ceiling((Bounds.Height - offsetY) / Scale));

        if (!(Bounds.Width / Scale > Settings.GridMaxSize || Bounds.Height / Scale > Settings.GridMaxSize))
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

        var (bmpW, bmpH, offsetX, offsetY) = CanvasHelper.GetBitmapRenderInfo(Scale, Offset, Bounds, Model);
        if (bmpW <= 0 || bmpH <= 0) return;

        foreach (var layer in LayerManager.Layers.Reverse())
        {
            if (!RenderCache.TryGetValue(layer, out var cache) || !layer.IsVisible || layer.IsEmpty) continue;

            if (cache.RenderBitmapDirty && cache.DirtyRect is Rect rect)
            {
                BitmapService.UpdateBitmap(layer.RenderBitmap, layer.PixelData, rect);

                cache.RenderBitmapDirty = false;
                cache.PreviewDirty = true;
                cache.DirtyRect = null;
            }

            if (Scale < 1)
                PreviewService.EnsurePreviewBitmap(this, layer, InvalidateVisual, bmpW, bmpH);
            else if (layer.PreviewBitmap != null)
                layer.PreviewBitmap = null;
        }

        context.DrawRectangle(new SolidColorBrush(Colors.Transparent), null, 
            new Rect(offsetX, offsetY, Model.Width * Scale, Model.Height * Scale));

        foreach (var layer in LayerManager.Layers.Reverse())
            if (layer.IsVisible && !layer.IsEmpty)
                DrawBitmap(context, layer, offsetX, offsetY);

        if (Scale >= 1)
        {
            DrawHoverPixel(context, offsetX, offsetY);
            DrawGrid(context, offsetX, offsetY, bmpW, bmpH);
        }
    }
}
