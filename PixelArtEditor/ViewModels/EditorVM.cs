using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.Other;
using PixelArtEditor.UI;
using ReactiveUI;
using System;
using System.Numerics;

namespace PixelArtEditor.ViewModels;

public class EditorVM : ReactiveObject
{
    private Canvas? _canvas;

    public WriteableBitmap? GetBitmap() => _canvas?.GetBitmap();

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
        set => this.RaiseAndSetIfChanged(ref _scale, value);
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
    
    private CreateParams _parameters;
    public CreateParams Parameters
    {
        get => _parameters;
        set => this.RaiseAndSetIfChanged(ref _parameters, value);
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

    private Color _pickedColor;
    public Color PickedColor
    {
        get => _pickedColor;
        set => this.RaiseAndSetIfChanged(ref _pickedColor, value);
    }

    public string? IndicatorText { get; set; }

    private WriteableBitmap? _importedBitmap;
    public WriteableBitmap? ImportedBitmap
    {
        get => _importedBitmap;
        set => this.RaiseAndSetIfChanged(ref _importedBitmap, value);
    }

    public void SetCanvas(Canvas canvas)
    {
        _canvas = canvas;

        _canvas.WhenAnyValue(x => x.CurrentPixelCoord)
            .Subscribe(coord =>
            {
                IndicatorText = coord is null
                    ? "X: - Y: -"
                    : $"X: {coord.Value.X} Y: {coord.Value.Y}";

                this.RaisePropertyChanged(nameof(IndicatorText));
            });
    }

    public EditorVM(CreateParams parameters)
    {
        _parameters = parameters;
        SetInitCreateParams(_parameters);
    }

    public EditorVM(WriteableBitmap bitmap)
    {
        _parameters = new CreateParams
        {
            Width = 32,
            Height = 32,
            BackgroundColor = Colors.White
        };

        SetInitBitmap(bitmap);
    }

    public void SetInitCreateParams(CreateParams parameters)
    {
        Parameters = parameters;
        ImportedBitmap = null;
        SelectedTool = ToolType.None;

        AdjustCanvas(_lastPanelWidth, _lastPanelHeight);
    }

    public void SetInitBitmap(WriteableBitmap bitmap)
    {
        _parameters = new CreateParams
        {
            Width = (short)bitmap.Size.Width,
            Height = (short)bitmap.Size.Height,
            BackgroundColor = Colors.White
        };
        ImportedBitmap = bitmap;
        SelectedTool = ToolType.None;

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
        if (width <= 0 || height <= 0 || _parameters.Width <= 0 || _parameters.Height <= 0) return;

        var borderSize = _parameters.Width > _parameters.Height ? width : height;
        var canvasSize = _parameters.Width > _parameters.Height ? _parameters.Width : _parameters.Height;

        MinScale = borderSize / canvasSize * 0.8;
        MaxScale = Math.Ceiling(borderSize / 8 * 10) / 10.0;

        if (MaxScale < MinScale)
        {
            MaxScale = MinScale;
        }

        Scale = MinScale;
        BaseScale = MinScale;
        Offset = Vector2.Zero;

        _lastPanelWidth = width;
        _lastPanelHeight = height;
    }
}