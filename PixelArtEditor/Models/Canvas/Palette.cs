using Avalonia.Media;
using System.Collections.Generic;

namespace PixelArtEditor.Models.Canvas;

public enum PaletteQuantization { Octree, MedianCut, KMeans }

public class Palette(List<Color> colors, PaletteQuantization? quantizationMethod = null, bool dither = false)
{
    public List<Color> Colors
    {
        get => colors;
        set
        {
            colors = value;
            Cube = null;
        }
    }

    public PaletteQuantization? QuantizationMethod { get; set; } = quantizationMethod;
    public bool Dither { get; set; } = dither;
    public Color[]? Cube { get; set; }
}