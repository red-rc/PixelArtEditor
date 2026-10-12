using Pixeller.AppServices.ImageProcessing.Icc;
using System;
using System.Threading.Tasks;

namespace Pixeller.AppServices.ImageProcessing;

/// <summary>
/// Conversions between the editor's BGRA (sRGB) buffer and interleaved 8-bit CMYK (255 = full ink).
/// With a usable CMYK ICC profile the A2B0 / B2A0 tables are used, otherwise a naive formula.
/// </summary>
public static class CmykConverter
{
    private const int Chunk = 4096;

    /// <summary>
    /// BGRA -> interleaved CMYK. Transparent pixels are composited onto white since CMYK has no alpha.
    /// </summary>
    public static byte[] FromBgra(byte[] bgra, byte[]? icc = null)
    {
        var engine = IccCmykConverter.TryCreate(icc);
        return engine is { CanExport: true } ? engine.BgraToCmyk(bgra) : FromBgraNaive(bgra);
    }

    /// <summary>
    /// Interleaved CMYK -> opaque BGRA. Uses the profile's A2B0 table when available.
    /// </summary>
    public static byte[] ToBgra(byte[] cmyk, byte[]? icc = null)
    {
        var engine = IccCmykConverter.TryCreate(icc);
        if (engine is { CanImport: true }) return engine.CmykToBgra(cmyk, cmyk.Length / 4);

        var pixels = cmyk.Length / 4;
        var res = new byte[pixels * 4];
        Parallel.For(0, (pixels + Chunk - 1) / Chunk, c =>
        {
            var end = Math.Min(pixels, (c + 1) * Chunk);
            for (var i = c * Chunk; i < end; i++)
            {
                var o = i * 4;
                int k = cmyk[o + 3];
                res[o] = Channel(cmyk[o + 2], k);
                res[o + 1] = Channel(cmyk[o + 1], k);
                res[o + 2] = Channel(cmyk[o], k);
                res[o + 3] = 255;
            }
        });
        return res;
    }

    private static byte[] FromBgraNaive(byte[] bgra)
    {
        var pixels = bgra.Length / 4;
        var res = new byte[pixels * 4];

        Parallel.For(0, (pixels + Chunk - 1) / Chunk, c =>
        {
            var end = Math.Min(pixels, (c + 1) * Chunk);
            for (var i = c * Chunk; i < end; i++)
            {
                var o = i * 4;
                int a = bgra[o + 3];
                int b = bgra[o], g = bgra[o + 1], r = bgra[o + 2];

                if (a != 255)
                {
                    r = (r * a + 255 * (255 - a) + 127) / 255;
                    g = (g * a + 255 * (255 - a) + 127) / 255;
                    b = (b * a + 255 * (255 - a) + 127) / 255;
                }

                var k = 255 - Math.Max(r, Math.Max(g, b));
                if (k == 255)
                {
                    res[o + 3] = 255;
                    continue;
                }

                var inv = 255 - k;
                res[o] = (byte)(((255 - r - k) * 255 + inv / 2) / inv);
                res[o + 1] = (byte)(((255 - g - k) * 255 + inv / 2) / inv);
                res[o + 2] = (byte)(((255 - b - k) * 255 + inv / 2) / inv);
                res[o + 3] = (byte)k;
            }
        });

        return res;
    }

    public static byte Channel(int ink, int k) => (byte)(((255 - ink) * (255 - k) + 127) / 255);
}
