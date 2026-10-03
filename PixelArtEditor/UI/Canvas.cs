using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Settings;
using PixelArtEditor.AppServices.Tools;
using PixelArtEditor.AppServices.Tools.Implementations;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace PixelArtEditor.UI;

public class Canvas : Control, ICanvasContext
{
    private static ISettingsManager Settings => Services.Settings;
    private Pen _gridPen = new(new SolidColorBrush(ColorHelper.HexToColor(Settings.GridColor)));

    public static readonly StyledProperty<PixelModel> ModelProperty =
        AvaloniaProperty.Register<Canvas, PixelModel>(nameof(Model));

    public PixelModel Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
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

    public Color DrawColor => Model is null
        ? PickedColor
        : ColorResolver.Resolve(PickedColor, Model.ColorMode, Model.Palette, Model.BitDepth);

    public LayerManager LayerManager { get; private set; } = null!;
    public Dictionary<LayerModel, LayerRenderCache> RenderCache { get; } = [];
    private CompositeDisposable? _layersSubscription;

    public Canvas()
    {
        Settings.WhenAnyValue(x => x.InterpolationMode, x => x.InterpolateOnlyWhenScalingDown).Subscribe(_ =>
        {
            UpdateInterpolationMode();
            InvalidateVisual();
        });

        Settings.WhenAnyValue(x => x.GridColor).Subscribe(_ =>
        {
            _gridPen = new(new SolidColorBrush(ColorHelper.HexToColor(Settings.GridColor)));
            InvalidateVisual();
        });

        this.WhenAnyValue(x => x.Model)
            .Where(model => model is not null)
            .Select(model => model.WhenAnyValue(x => x.Width, x => x.Height))
            .Switch()
            .Subscribe(_ => {
                LayerManager.ResizeLayers(Model.Width, Model.Height);
                InvalidateVisual();
            });

        this.GetObservable(SelectedToolProperty).Subscribe(newTool => _currentTool = ToolManager.Get(newTool));
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

    public void AttachLayerManager(LayerManager layerManager)
    {
        _layersSubscription?.Dispose();
        RenderCache.Clear();
        LayerManager = layerManager;

        var changeSet = LayerManager.Layers.ToObservableChangeSet();

        _layersSubscription = new CompositeDisposable(
            changeSet
                .OnItemAdded(layer => RenderCache[layer] = new LayerRenderCache { RenderBitmapDirty = false })
                .OnItemRemoved(layer => RenderCache.Remove(layer))
                .Subscribe(_ => InvalidateVisual()),
            changeSet
                .MergeMany(layer => layer.WhenAnyValue(x => x.ThumbDirtyRect).Where(rect => rect is not null))
                .Subscribe(_ =>
                {
                    _hoverPixelColor = null;
                    InvalidateVisual();
                }),
            changeSet
                .MergeMany(layer => layer.WhenAnyValue(x => x.IsVisible, x => x.Opacity))
                .Subscribe(_ => InvalidateVisual())
        );
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

    private void DrawHoverPixel(DrawingContext context, double offsetX, double offsetY)
    {
        if (HoverPixel is null) return;

        var rect = new Rect(
            offsetX + HoverPixel.Value.X * Scale,
            offsetY + HoverPixel.Value.Y * Scale,
            Scale, Scale);

        _hoverPixelColor
            ??= CanvasHelper.GetHighlightColor(BitmapService.GetCompositePixelColor(LayerManager.Layers, HoverPixel.Value));

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
                var posX = offsetX + x * Scale;
                context.DrawLine(_gridPen, new Point(posX, offsetY), new Point(posX, offsetY + bmpH));
            }

            for (var y = startY; y <= endY; y++)
            {
                var posY = offsetY + y * Scale;
                context.DrawLine(_gridPen, new Point(offsetX, posY), new Point(offsetX + bmpW, posY));
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
            if (!RenderCache.TryGetValue(layer, out var cache) || !layer.IsVisible) continue;

            if (cache.RenderBitmapDirty && cache.DirtyRect is PixelRect dirtyRect)
            {
                layer.Tiles.Update(layer.Data, dirtyRect);

                cache.RenderBitmapDirty = false;
                cache.DirtyRect = null;
            }
        }

        context.DrawRectangle(new SolidColorBrush(Colors.Transparent), null, 
            new Rect(offsetX, offsetY, Model.Width * Scale, Model.Height * Scale));

        foreach (var layer in LayerManager.Layers.Reverse())
            if (layer.IsVisible)
                layer.Tiles.Draw(context, offsetX, offsetY, Scale, new Rect(Bounds.Size), layer.Opacity);

        if (Scale >= 1)
        {
            DrawHoverPixel(context, offsetX, offsetY);
            DrawGrid(context, offsetX, offsetY, bmpW, bmpH);
        }
    }
}