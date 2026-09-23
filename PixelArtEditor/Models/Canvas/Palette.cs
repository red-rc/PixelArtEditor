using Avalonia.Media;
using System.Collections.Generic;

namespace PixelArtEditor.Models.Canvas;

public enum PaletteQuantization { Octree, MedianCut, KMeans }

public class Palette(List<Color> colors, PaletteQuantization? quantizationMethod = null, bool? dither = null)
{
    public List<Color> Colors { get; set; } = colors;
    public PaletteQuantization? QuantizationMethod = quantizationMethod;
    public bool? Dither = dither;
}