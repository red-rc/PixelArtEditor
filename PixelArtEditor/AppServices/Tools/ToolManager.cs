using Avalonia;
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

    public static void InvalidatePixelData(ICanvasContext ctx, LayerModel layer, Rect dirtyRect)
    {
        ctx.RenderCache[layer].RenderBitmapDirty = true;
        ctx.RenderCache[layer].PreviewDirty = true;
        ctx.RenderCache[layer].DirtyRect = ctx.RenderCache[layer].DirtyRect is Rect existing
            ? existing.Union(dirtyRect) : dirtyRect;

        layer.NotifyPixelDataChanged();
    }
}
