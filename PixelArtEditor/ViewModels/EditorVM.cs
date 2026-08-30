using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Tools;
using PixelArtEditor.Controls.Editor;
using PixelArtEditor.Models.Canvas;
using System;
using System.Numerics;

namespace PixelArtEditor.ViewModels;

public class EditorVM : ReactiveObject
{
    private double _dragBgOpacity = 0;
    public double DragBgOpacity
    {
        get => _dragBgOpacity;
        set => this.RaiseAndSetIfChanged(ref _dragBgOpacity, value);
    }

    private bool _dragImageVisible = false;
    public bool DragImageVisible
    {
        get => _dragImageVisible;
        set => this.RaiseAndSetIfChanged(ref _dragImageVisible, value);
    }

    private bool _confirmPanelVisible = false;
    public bool ConfirmPanelVisible
    {
        get => _confirmPanelVisible;
        set => this.RaiseAndSetIfChanged(ref _confirmPanelVisible, value);
    }

    private Canvas? _canvas;
    public Canvas? Canvas
    {
        get => _canvas;
        set => this.RaiseAndSetIfChanged(ref _canvas, value);
    }

    private PixelModel _model;
    public PixelModel Model
    {
        get => _model;
        set => this.RaiseAndSetIfChanged(ref _model, value);
    }

    public PixelModel GetPreparedModel()
    {
        if (_canvas is not null)
        {
            _model.Data = LayerManager.GetCompositePixelData(Model.Width, Model.Height);
            this.RaisePropertyChanged(nameof(Model));
        }

        return _model;
    }

    private LayerManager _layerManager = new ();
    public LayerManager LayerManager
    {
        get => _layerManager;
        private set => this.RaiseAndSetIfChanged(ref _layerManager, value);
    }

    private double _lastPanelWidth = -1;
    private double _lastPanelHeight = -1;

    private Point _startMousePosition;
    private Vector2 _startOffset;
    public bool IsPositionSet;
    
    private Vector2 _offset;
    public Vector2 Offset
    {
        get => _offset;
        set => this.RaiseAndSetIfChanged(ref _offset, value);
    }

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

    private int _maxScale;
    public int MaxScale
    {
        get => _maxScale;
        private set => this.RaiseAndSetIfChanged(ref _maxScale, value);
    }

    public double BaseScale { get; private set; }

    public ToolSelection Tools { get; private set; } = new();

    private Color _pickedColor = Colors.White;
    public Color PickedColor
    {
        get => _pickedColor;
        set => this.RaiseAndSetIfChanged(ref _pickedColor, value);
    }

    public string? CoordinatesText { get; set; }
    public string? ScaleText { get; set; }

    private readonly ObservableAsPropertyHelper<bool> _isTransforming;
    public bool IsTransforming => _isTransforming.Value;

    public void SetCanvas(Canvas canvas)
    {
        _canvas = canvas;

        LayerManager.InitializeFirstLayer(
            _model.Width, 
            _model.Height, 
            _model.Data, 
            _model.Name ?? $"Layer {LayerManager.Layers.Count + 1}");

        canvas.AttachLayerManager(LayerManager);

        _canvas.WhenAnyValue(x => x.CurrentPixelCoord).Subscribe(coord =>
        {
            CoordinatesText = coord is null
                ? "X: - Y: -"
                : $"X: {coord.Value.X} Y: {coord.Value.Y}";

            this.RaisePropertyChanged(nameof(CoordinatesText));
        });
    }

    public EditorVM(PixelModel model)
    {
        _model = model;
        ConfirmPanelVisible = false;

        this.WhenAnyValue(vm => vm.ConfirmPanelVisible)
            .ToProperty(this, vm => vm.IsTransforming, out _isTransforming);

        AdjustCanvas(_lastPanelWidth, _lastPanelHeight);
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