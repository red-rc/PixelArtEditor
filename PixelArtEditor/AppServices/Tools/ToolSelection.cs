using PixelArtEditor.Models.Tools;

namespace PixelArtEditor.AppServices.Tools;

public class ToolSelection : ReactiveObject
{
    private ToolType _selectedTool;
    public ToolType SelectedTool
    {
        get => _selectedTool;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTool, value);

            foreach (var property in GetType().GetProperties())
            {
                if (property.Name != nameof(SelectedTool))
                    this.RaisePropertyChanged(property.Name);
            }
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
}
