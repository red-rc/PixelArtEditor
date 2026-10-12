using Avalonia.Media;
using Pixeller.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pixeller.AppServices.ImageProcessing;

public static class ColorSpaceConverter
{
    public static byte[] ConvertData(byte[] bgra, ColorSpace src, ColorSpace dst)
    {
        if (src == dst || src == ColorSpace.Custom || dst == ColorSpace.Custom
            || src.Family is ColorFamily.None or ColorFamily.Cmyk
            || dst.Family is ColorFamily.None or ColorFamily.Cmyk) return bgra;

        double[]? m = null;
        if (src.Family == ColorFamily.Rgb && dst.Family == ColorFamily.Rgb
            && src.RgbToXyz is { } a && dst.RgbToXyz is { } b && !ReferenceEquals(a, b))
            m = Mul(Inv(b), a);

        if (m is null && src.Transfer == dst.Transfer) return bgra;

        var dec = new float[256];
        for (var i = 0; i < 256; i++) dec[i] = Decode(src.Transfer, i / 255f);

        var enc = new byte[4096];
        for (var i = 0; i < 4096; i++)
            enc[i] = (byte)Math.Clamp((int)MathF.Round(Encode(dst.Transfer, i / 4095f) * 255f), 0, 255);

        var mf = m is null ? null : Array.ConvertAll(m, v => (float)v);
        var res = new byte[bgra.Length];
        var pixels = bgra.Length / 4;
        const int chunk = 4096;

        Parallel.For(0, (pixels + chunk - 1) / chunk, c =>
        {
            var end = Math.Min(pixels, (c + 1) * chunk);
            for (var i = c * chunk; i < end; i++)
            {
                var o = i * 4;
                float r = dec[bgra[o + 2]], g = dec[bgra[o + 1]], bl = dec[bgra[o]];

                if (mf is not null)
                {
                    var nr = mf[0] * r + mf[1] * g + mf[2] * bl;
                    var ng = mf[3] * r + mf[4] * g + mf[5] * bl;
                    var nb = mf[6] * r + mf[7] * g + mf[8] * bl;
                    r = nr; g = ng; bl = nb;
                }

                res[o + 2] = enc[(int)(Math.Clamp(r, 0f, 1f) * 4095f + 0.5f)];
                res[o + 1] = enc[(int)(Math.Clamp(g, 0f, 1f) * 4095f + 0.5f)];
                res[o] = enc[(int)(Math.Clamp(bl, 0f, 1f) * 4095f + 0.5f)];
                res[o + 3] = bgra[o + 3];
            }
        });

        return res;
    }

    public static List<Color> ConvertPalette(IReadOnlyList<Color> colors, ColorSpace src, ColorSpace dst)
    {
        var bgra = new byte[colors.Count * 4];
        for (var i = 0; i < colors.Count; i++)
        {
            var c = colors[i];
            bgra[i * 4] = c.B; bgra[i * 4 + 1] = c.G; bgra[i * 4 + 2] = c.R; bgra[i * 4 + 3] = c.A;
        }

        var res = ConvertData(bgra, src, dst);

        var result = new List<Color>(colors.Count);
        for (var i = 0; i < colors.Count; i++)
            result.Add(Color.FromArgb(res[i * 4 + 3], res[i * 4 + 2], res[i * 4 + 1], res[i * 4]));
        return result;
    }

    private static float Decode(TransferFunction t, float v) => t switch
    {
        TransferFunction.SRgb => v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f),
        TransferFunction.Gamma22 => MathF.Pow(v, 2.2f),
        TransferFunction.Gamma18 => MathF.Pow(v, 1.8f),
        _ => v
    };

    private static float Encode(TransferFunction t, float v) => t switch
    {
        TransferFunction.SRgb => v <= 0.0031308f ? v * 12.92f : 1.055f * MathF.Pow(v, 1f / 2.4f) - 0.055f,
        TransferFunction.Gamma22 => MathF.Pow(v, 1f / 2.2f),
        TransferFunction.Gamma18 => MathF.Pow(v, 1f / 1.8f),
        _ => v
    };

    private static double[] Mul(double[] a, double[] b)
    {
        var c = new double[9];
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
                for (var k = 0; k < 3; k++)
                    c[i * 3 + j] += a[i * 3 + k] * b[k * 3 + j];
        return c;
    }

    private static double[] Inv(double[] a)
    {
        var det = a[0] * (a[4] * a[8] - a[5] * a[7]) - a[1] * (a[3] * a[8] - a[5] * a[6]) + a[2] * (a[3] * a[7] - a[4] * a[6]);
        return
        [
            (a[4] * a[8] - a[5] * a[7]) / det, (a[2] * a[7] - a[1] * a[8]) / det, (a[1] * a[5] - a[2] * a[4]) / det,
            (a[5] * a[6] - a[3] * a[8]) / det, (a[0] * a[8] - a[2] * a[6]) / det, (a[2] * a[3] - a[0] * a[5]) / det,
            (a[3] * a[7] - a[4] * a[6]) / det, (a[1] * a[6] - a[0] * a[7]) / det, (a[0] * a[4] - a[1] * a[3]) / det
        ];
    }
}