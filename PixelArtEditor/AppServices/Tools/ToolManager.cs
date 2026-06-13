using Avalonia.Media;
using PixelArtEditor.AppServices.Tools.Implementations;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Tools;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices.Tools;

public static class ToolManager
{
    private static readonly Dictionary<ToolType, ITool> _tools = new()
    {
        [ToolType.Pen] = new PenTool(),
        [ToolType.Eraser] = new EraserTool(),
        [ToolType.ColorPicker] = new ColorPickerTool(),
        [ToolType.Fill] = new FillTool(),
        [ToolType.Hand] = new HandTool(),
        [ToolType.None] = new EmptyTool(),
    };

    public static ITool Get(ToolType type) => _tools[type];

    public static void UpdatePixelData(ICanvasContext context, int x, int y, Color color)
    {
        var layer = context.ActiveLayer;
        if (layer is null || layer.PixelData is null) return;

        var stride = context.Model.Width * 4;
        var index = y * stride + x * 4;

        if (color.A == 0)
        {
            layer.PixelData[index + 0] = 0;
            layer.PixelData[index + 1] = 0;
            layer.PixelData[index + 2] = 0;
            layer.PixelData[index + 3] = 0;
        }
        else
        {
            layer.PixelData[index + 0] = color.B;
            layer.PixelData[index + 1] = color.G;
            layer.PixelData[index + 2] = color.R;
            layer.PixelData[index + 3] = color.A;
        }

        context.RenderCache[layer].RenderBitmapDirty = true;
        context.RenderCache[layer].PreviewDirty = true;
        layer.NotifyPixelDataChanged();
    }
}
