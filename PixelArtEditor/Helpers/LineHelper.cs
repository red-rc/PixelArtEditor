using Avalonia;
using System;
using System.Collections.Generic;

namespace PixelArtEditor.Helpers;

public static class LineHelper
{
    public static IEnumerable<PixelPoint> GetLine(PixelPoint from, PixelPoint to)
    {
        var x0 = from.X;
        var y0 = from.Y;
        var x1 = to.X;
        var y1 = to.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);

        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;

        var error = dx - dy;

        while (true)
        {
            yield return new PixelPoint(x0, y0);

            if (x0 == x1 && y0 == y1)
                break;

            var error2 = 2 * error;

            if (error2 > -dy)
            {
                error -= dy;
                x0 += sx;
            }

            if (error2 < dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }
}
