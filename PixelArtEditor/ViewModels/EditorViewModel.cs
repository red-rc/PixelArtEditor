using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using PixelArtEditor.Other;
using ReactiveUI;

namespace PixelArtEditor.ViewModels;

public class EditorViewModel : ReactiveObject
{
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
    
    private Color _pickedColor;
    public Color PickedColor
    {
        get => _pickedColor;
        set => this.RaiseAndSetIfChanged(ref _pickedColor, value);
    }

    public EditorViewModel(CreateParams parameters)
    {
        _parameters = parameters;
        SelectedTool = ToolType.None;
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
    
    public void CalculateScale(double width, double height)
    {
        var borderSize = _parameters.Width > _parameters.Height ? width : height;
        var canvasSize = _parameters.Width > _parameters.Height ? _parameters.Width : _parameters.Height;
        
        var scale = Math.Ceiling(borderSize / canvasSize * 10) / 10.0;

        MinScale = scale * 0.9;
        MaxScale = Math.Ceiling(borderSize / 8 * 10) / 10.0;
        Scale = MinScale;
        BaseScale = MinScale;
    }
}