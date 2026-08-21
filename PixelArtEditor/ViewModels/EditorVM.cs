using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Controls.Editor;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;
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
    
    private double _scale;
    public double Scale
    {
        get => _scale;
        set
        {
            this.RaiseAndSetIfChanged(ref _scale, value);
            UpdateScaleText();
            this.RaisePropertyChanged(nameof(ScaleText));
        }
    }
    
    private double _baseScale;
    public double BaseScale
    {
        get => _baseScale;
        private set => this.RaiseAndSetIfChanged(ref _baseScale, value);
    }
    
    private double _minScale;
    public double MinScale
    {
        get => _minScale;
        private set => this.RaiseAndSetIfChanged(ref _minScale, value);
    }
    
    private double _maxScale;
    public double MaxScale
    {
        get => _maxScale;
        private set => this.RaiseAndSetIfChanged(ref _maxScale, value);
    }
    
    private ToolType _selectedTool;
    public ToolType SelectedTool
    {
        get => _selectedTool;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTool, value);
            this.RaisePropertyChanged(nameof(IsPenEnabled));
            this.RaisePropertyChanged(nameof(IsColorPickerEnabled));
            this.RaisePropertyChanged(nameof(IsFillEnabled));
            this.RaisePropertyChanged(nameof(IsEraserEnabled));
            this.RaisePropertyChanged(nameof(IsHandEnabled));
        }
    }

    public bool IsPenEnabled
    {
        get => SelectedTool == ToolType.Pen;
        set
        {
            if (value)
                SelectedTool = ToolType.Pen;
            else if (SelectedTool == ToolType.Pen)
                SelectedTool = ToolType.None;
        }
    }

    public bool IsColorPickerEnabled
    {
        get => SelectedTool == ToolType.ColorPicker;
        set
        {
            if (value)
                SelectedTool = ToolType.ColorPicker;
            else if (SelectedTool == ToolType.ColorPicker)
                SelectedTool = ToolType.None;
        }
    }

    public bool IsFillEnabled
    {
        get => SelectedTool == ToolType.Fill;
        set
        {
            if (value)
                SelectedTool = ToolType.Fill;
            else if (SelectedTool == ToolType.Fill)
                SelectedTool = ToolType.None;
        }
    }

    public bool IsEraserEnabled
    {
        get => SelectedTool == ToolType.Eraser;
        set
        {
            if (value)
                SelectedTool = ToolType.Eraser;
            else if (SelectedTool == ToolType.Eraser)
                SelectedTool = ToolType.None;
        }
    }

    public bool IsHandEnabled
    {
        get => SelectedTool == ToolType.Hand;
        set
        {
            if (value)
                SelectedTool = ToolType.Hand;
            else if (SelectedTool == ToolType.Hand)
                SelectedTool = ToolType.None;
        }
    }

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

    public void ZoomBy(float factor)
    {
        if (_lastPanelWidth <= 0 || _lastPanelHeight <= 0)
        {
            Scale = Math.Clamp(Scale * factor, MinScale, MaxScale);
            return;
        }

        var oldScale = Scale;
        var newScale = Math.Clamp(oldScale * factor, MinScale, MaxScale);

        if (Math.Abs(newScale - oldScale) < 1e-9) return;

        var screenVec = new Vector2(Offset.X, Offset.Y);
        var newScreenVec = screenVec / (float)oldScale * (float)newScale;

        var correctedOffset = new Vector2(
            Offset.X + newScreenVec.X - screenVec.X,
            Offset.Y + newScreenVec.Y - screenVec.Y);

        Scale = newScale;
        Offset = correctedOffset;
    }

    public void AdjustCanvas(double width, double height)
    {
        var modelWidth = Model?.Width ?? 0;
        var modelHeight = Model?.Height ?? 0;

        if (width <= 0 || height <= 0 || modelWidth <= 0 || modelHeight <= 0) return;

        var borderSize = modelWidth > modelHeight ? width : height;
        var canvasSize = modelWidth > modelHeight ? modelWidth : modelHeight;

        MinScale = borderSize / canvasSize * 0.5;
        MaxScale = Math.Ceiling(borderSize / 8 * 10) / 10.0;

        BaseScale = borderSize / canvasSize * 0.8;
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
        var logCur = Math.Log(Math.Clamp(_scale, MinScale, MaxScale));

        var progress = (logCur - logMin) / (logMax - logMin);
        ScaleText = $"{progress * 100:0}%";
    }
}