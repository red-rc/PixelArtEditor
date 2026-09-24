using Avalonia.Media;
using PixelArtEditor.Models.Canvas;

namespace PixelArtEditor.AppServices.Bitmap;

public static class PaletteLookup
{
    private const int Bits = 5; // 32
    private const int Levels = 1 << Bits; // 32
    private const int Shift = 8 - Bits;    // 3

    public static Color[] BuildLookupCube(Palette palette)
    {
        var cube = new Color[Levels * Levels * Levels];

        for (var ri = 0; ri < Levels; ri++)
            for (var gi = 0; gi < Levels; gi++)
                for (var bi = 0; bi < Levels; bi++)
                {
                    var r = (byte)((ri << Shift) | (ri >> (Bits - Shift)));
                    var g = (byte)((gi << Shift) | (gi >> (Bits - Shift)));
                    var b = (byte)((bi << Shift) | (bi >> (Bits - Shift)));

                    var idx = (ri * Levels + gi) * Levels + bi;
                    cube[idx] = PaletteService.GetClosestPaletteColor(Color.FromArgb(255, r, g, b), palette);
                }

        return cube;
    }

    public static Color Lookup(Color[] cube, Color color)
    {
        var ri = color.R >> Shift;
        var gi = color.G >> Shift;
        var bi = color.B >> Shift;

        var idx = (ri * Levels + gi) * Levels + bi;
        var c = cube[idx];

        return Color.FromArgb(color.A, c.R, c.G, c.B);
    }
}
