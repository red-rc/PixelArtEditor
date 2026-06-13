using Avalonia;
using Avalonia.Media;
using PixelArtEditor.AppServices.Canvas;
using System.Collections.Generic;
using System.Numerics;

namespace PixelArtEditor.Models.Canvas;

public interface ICanvasContext
{
    PixelModel Model { get; }
    PixelPoint? HoverPixel { get; set; }
    PixelPoint? CurrentPixelCoord { get; set; }
    Color PickedColor { get; set; }

    LayerManager LayerManager { get; }
    Dictionary<LayerModel, LayerRenderCache> RenderCache { get; }

    Vector2 Offset { get; }
    double Scale { get; }
    Rect Bounds { get; }
}