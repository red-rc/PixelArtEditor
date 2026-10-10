using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Tools;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.UI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PixelArtEditor.ViewModels;

public partial class EditorVM : ReactiveObject
{
    [Reactive] public partial double DragBgOpacity { get; set; } = 0;
    [Reactive] public partial bool DragImageVisible { get; set; } = false;
    [Reactive] public partial bool ConfirmPanelVisible { get; set; } = false;
    [Reactive] public partial Canvas? Canvas { get; set; }
    [Reactive] public partial PixelModel Model { get; set; }

    public PixelModel GetPreparedModel()
    {
        return new PixelModel
        {
            Name = Model.Name,
            Extension = Model.Extension,
            Width = Model.Width,
            Height = Model.Height,
            ColorMode = Model.ColorMode,
            BitDepth = Model.BitDepth,
            ColorSpace = Model.ColorSpace,
            SrcColorSpace = Model.SrcColorSpace,
            AlphaFormat = Model.AlphaFormat,
            DpiX = Model.DpiX,
            DpiY = Model.DpiY,
            Palette = Model.Palette,
            Data = LayerManager.GetCompositePixelData(Model.Width, Model.Height),
            DicomDataset = Model.DicomDataset,
            IccProfile = Model.IccProfile
        };
    }

    private LayerManager _layerManager = new();
    public LayerManager LayerManager
    {
        get => _layerManager;
        private set => this.RaiseAndSetIfChanged(ref _layerManager, value);
    }

    public event Action<List<LayerModel>, EditorVM>? LayersPasted;
    public void NotifyLayersPasted(List<LayerModel> layers) => LayersPasted?.Invoke(layers, this);

    private double _lastPanelWidth = -1;
    private double _lastPanelHeight = -1;

    private Point _startMousePosition;
    private Vector2 _startOffset;
    public bool IsPositionSet;
    
    [Reactive] public partial Vector2 Offset { get; set; }

    private const int ScaleStep = 8;

    private double _scale;
    public double Scale
    {
        get => _scale;
        set
        {
            this.RaiseAndSetIfChanged(ref _scale, Math.Clamp(value, MinScale, MaxScale));
            this.RaisePropertyChanged(nameof(Scale));
            UpdateScaleText();
            this.RaisePropertyChanged(nameof(ScaleText));
        }
    }

    public double MinScale { get; private set; }
    [Reactive] public partial int MaxScale { get; set; }
    public double BaseScale { get; private set; }

    public ToolSelection Tools { get; private set; } = new();
    [Reactive] public partial Color PickedColor { get; set; } = Colors.Red;

    [Reactive] public partial string? CoordinatesText { get; set; }
    [Reactive] public partial string? ScaleText { get; set; }

    private readonly ObservableAsPropertyHelper<bool> _isTransforming;
    public bool IsTransforming => _isTransforming.Value;

    public void SetCanvas(Canvas canvas)
    {
        Canvas = canvas;

        LayerManager.InitializeFirstLayer(
            Model.Width,
            Model.Height,
            Model.Data,
            Model.Name ?? $"{LocalizationService.Get("Layer")} {LayerManager.Layers.Count + 1}");

        canvas.AttachLayerManager(LayerManager);

        Canvas.WhenAnyValueUnsafe(x => x.HoverPixel).Subscribe(coord =>
        {
            CoordinatesText = coord is null
                ? "X: - Y: -"
                : $"X: {coord.Value.X} Y: {coord.Value.Y}";

            this.RaisePropertyChanged(nameof(CoordinatesText));
        });
    }

    public EditorVM(PixelModel model)
    {
        Model = model;
        ConfirmPanelVisible = false;

        this.WhenAnyValueUnsafe(vm => vm.ConfirmPanelVisible).ToProperty(this, vm => vm.IsTransforming, out _isTransforming);
    }

    public void StartDragging(Point startMousePos)
    {
        _startMousePosition = startMousePos;
        _startOffset = Offset;
        IsPositionSet = true;
    }

    public void UpdateDragging(Point currentMousePos)
    {
        var dx = (float)(currentMousePos.X - _startMousePosition.X);
        var dy = (float)(currentMousePos.Y - _startMousePosition.Y);
        Offset = new Vector2(_startOffset.X + dx, _startOffset.Y + dy);
    }

    public void ZoomBy(float factor, bool applyCursorOffset = false, Point? mousePos = null, double scrollDelta = 0)
    {
        var oldEffective = Scale;

        if (scrollDelta != 0)
            Scale *= Math.Pow(factor, scrollDelta);
        else
            Scale *= factor;

        var newEffective = Scale;

        if (_lastPanelWidth <= 0 || _lastPanelHeight <= 0) return;

        var scaleRatio = (float)(newEffective / oldEffective);

        if (applyCursorOffset && mousePos is Point mouse)
        {
            var center = new Point(_lastPanelWidth / 2, _lastPanelHeight / 2);
            var screenVec = new Vector2(
                (float)mouse.X - (float)center.X - Offset.X,
                (float)mouse.Y - (float)center.Y - Offset.Y);

            Offset += screenVec * (1 - scaleRatio);
        }
        else
            Offset *= scaleRatio;
    }

    public void AdjustCanvas(double width, double height)
    {
        var modelWidth = Model?.Width ?? 0;
        var modelHeight = Model?.Height ?? 0;
        if (width <= 0 || height <= 0 || modelWidth <= 0 || modelHeight <= 0) return;

        var borderSize = modelWidth > modelHeight ? width : height;
        var minRatio = Math.Min(width / modelWidth, height / modelHeight);

        MinScale = minRatio * 0.5;
        MaxScale = (int)(Math.Ceiling(borderSize / 8 / ScaleStep) * ScaleStep);
        BaseScale = Math.Max(MinScale, minRatio * 0.8);

        Scale = BaseScale;
        Offset = Vector2.Zero;

        _lastPanelWidth = width;
        _lastPanelHeight = height;
    }

    private void UpdateScaleText()
    {
        if (MinScale <= 0 || MaxScale <= MinScale)
        {
            ScaleText = "0%";
            return;
        }

        var logMin = Math.Log(MinScale);
        var logMax = Math.Log(MaxScale);
        var logCur = Math.Log(Math.Clamp(Scale, MinScale, MaxScale));

        var progress = (logCur - logMin) / (logMax - logMin);
        ScaleText = $"{progress * 100:0}%";
    }
}
