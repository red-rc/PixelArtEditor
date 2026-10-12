using System;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.ImageProcessing.Icc;

/// <summary>
/// Minimal ICC engine: CMYK (device) &lt;-&gt; sRGB through the profile's A2B0 / B2A0 tables.
/// The PCS (D50 Lab or XYZ) is adapted to the sRGB D65 white point with the Bradford matrices baked into
/// the sRGB &lt;-&gt; XYZ(D50) matrices below. Editor data stays BGRA sRGB.
/// </summary>
public sealed class IccCmykConverter
{
    private const int Chunk = 2048;

    // Linear sRGB -> XYZ (D50, Bradford adapted) and its inverse.
    private static readonly float[] RgbToXyz =
    [
        0.4360747f, 0.3850649f, 0.1430804f,
        0.2225045f, 0.7168786f, 0.0606169f,
        0.0139322f, 0.0971045f, 0.7141733f
    ];

    private static readonly float[] XyzToRgb =
    [
        3.1338561f, -1.6168667f, -0.4906146f,
        -0.9787684f, 1.9161415f, 0.0334540f,
        0.0719453f, -0.2289914f, 1.4052427f
    ];

    private static readonly float[] SrgbDecode = BuildDecode();
    private static readonly byte[] SrgbEncode = BuildEncode();

    private readonly IccCmykProfile _profile;

    private IccCmykConverter(IccCmykProfile profile) => _profile = profile;

    public bool CanImport => _profile.A2B is not null;
    public bool CanExport => _profile.B2A is not null;

    /// <summary>Parses a CMYK ICC profile; null if it is not a usable CMYK profile.</summary>
    public static IccCmykConverter? TryCreate(byte[]? icc)
    {
        var profile = IccCmykProfile.TryParse(icc);
        return profile is null ? null : new IccCmykConverter(profile);
    }

    private static float[] BuildDecode()
    {
        var t = new float[256];
        for (var i = 0; i < 256; i++)
        {
            var v = i / 255f;
            t[i] = v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }
        return t;
    }

    private static byte[] BuildEncode()
    {
        var t = new byte[4097];
        for (var i = 0; i <= 4096; i++)
        {
            var v = i / 4096f;
            var e = v <= 0.0031308f ? v * 12.92f : 1.055f * MathF.Pow(v, 1f / 2.4f) - 0.055f;
            t[i] = (byte)Math.Clamp((int)MathF.Round(e * 255f), 0, 255);
        }
        return t;
    }

    // --- PCS encoding ---

    /// <summary>Normalised LUT output (0..1) -> XYZ (D50, Y of media white = 1).</summary>
    private static void PcsToXyz(PcsEncoding enc, float a, float b, float c, out float x, out float y, out float z)
    {
        if (enc == PcsEncoding.Xyz)
        {
            // u1Fixed15: 0x8000 == 1.0, stored in 0..65535
            const float scale = 65535f / 32768f;
            x = a * scale; y = b * scale; z = c * scale;
            return;
        }

        float L, A, B;
        switch (enc)
        {
            case PcsEncoding.Lab8:
                L = a * 100f; A = b * 255f - 128f; B = c * 255f - 128f; break;
            case PcsEncoding.LabLegacy16:
                L = a * 65535f / 65280f * 100f; A = b * 65535f / 256f - 128f; B = c * 65535f / 256f - 128f; break;
            default:
                L = a * 100f; A = b * 255f - 128f; B = c * 255f - 128f; break;
        }

        var fy = (L + 16f) / 116f;
        var fx = fy + A / 500f;
        var fz = fy - B / 200f;
        x = (float)IccCmykProfile.D50X * Finv(fx);
        y = Finv(fy);
        z = (float)IccCmykProfile.D50Z * Finv(fz);
    }

    private static void XyzToPcs(PcsEncoding enc, float x, float y, float z, out float a, out float b, out float c)
    {
        if (enc == PcsEncoding.Xyz)
        {
            const float scale = 32768f / 65535f;
            a = x * scale; b = y * scale; c = z * scale;
            return;
        }

        var fx = F(x / (float)IccCmykProfile.D50X);
        var fy = F(y);
        var fz = F(z / (float)IccCmykProfile.D50Z);
        var L = 116f * fy - 16f;
        var A = 500f * (fx - fy);
        var B = 200f * (fy - fz);

        switch (enc)
        {
            case PcsEncoding.LabLegacy16:
                a = L / 100f * 65280f / 65535f; b = (A + 128f) * 256f / 65535f; c = (B + 128f) * 256f / 65535f; break;
            default:
                a = L / 100f; b = (A + 128f) / 255f; c = (B + 128f) / 255f; break;
        }
    }

    private static float F(float t) => t > 216f / 24389f ? MathF.Cbrt(t) : (24389f / 27f * t + 16f) / 116f;
    private static float Finv(float f) => f * f * f > 216f / 24389f ? f * f * f : (116f * f - 16f) * 27f / 24389f;

    // --- CMYK -> BGRA ---

    /// <summary>
    /// Converts interleaved 8-bit CMYK (ink coverage, 255 = full ink) into opaque BGRA sRGB.
    /// </summary>
    public byte[] CmykToBgra(byte[] cmyk, int pixels)
    {
        var lut = _profile.A2B ?? throw new InvalidOperationException("Profile has no A2B table");
        var res = new byte[pixels * 4];

        Parallel.For(0, (pixels + Chunk - 1) / Chunk, chunkIndex =>
        {
            var end = Math.Min(pixels, (chunkIndex + 1) * Chunk);
            Span<float> input = stackalloc float[4];
            Span<float> pcs = stackalloc float[4];
            uint lastKey = 0; byte lr = 0, lg = 0, lb = 0; var hasLast = false;

            for (var i = chunkIndex * Chunk; i < end; i++)
            {
                var p = i * 4;
                var key = (uint)(cmyk[p] | cmyk[p + 1] << 8 | cmyk[p + 2] << 16 | cmyk[p + 3] << 24);
                if (!hasLast || key != lastKey)
                {
                    for (var c = 0; c < 4; c++) input[c] = cmyk[p + c] / 255f;
                    lut.Eval(input, pcs);
                    PcsToXyz(lut.Pcs, pcs[0], pcs[1], pcs[2], out var x, out var y, out var z);

                    var r = XyzToRgb[0] * x + XyzToRgb[1] * y + XyzToRgb[2] * z;
                    var g = XyzToRgb[3] * x + XyzToRgb[4] * y + XyzToRgb[5] * z;
                    var b = XyzToRgb[6] * x + XyzToRgb[7] * y + XyzToRgb[8] * z;

                    lr = Encode(r); lg = Encode(g); lb = Encode(b);
                    lastKey = key; hasLast = true;
                }

                var o = i * 4;
                res[o] = lb; res[o + 1] = lg; res[o + 2] = lr; res[o + 3] = 255;
            }
        });

        return res;
    }

    private static byte Encode(float linear) => SrgbEncode[(int)(Math.Clamp(linear, 0f, 1f) * 4096f + 0.5f)];

    // --- BGRA -> CMYK ---

    /// <summary>
    /// Converts BGRA sRGB into interleaved 8-bit CMYK (255 = full ink) via B2A0.
    /// Transparent pixels are composited onto white since CMYK has no alpha.
    /// </summary>
    public byte[] BgraToCmyk(byte[] bgra)
    {
        var lut = _profile.B2A ?? throw new InvalidOperationException("Profile has no B2A table");
        var pixels = bgra.Length / 4;
        var res = new byte[pixels * 4];

        Parallel.For(0, (pixels + Chunk - 1) / Chunk, chunkIndex =>
        {
            var end = Math.Min(pixels, (chunkIndex + 1) * Chunk);
            Span<float> input = stackalloc float[4];
            Span<float> ink = stackalloc float[4];
            uint lastKey = 0; var hasLast = false;
            uint lastOut = 0;

            for (var i = chunkIndex * Chunk; i < end; i++)
            {
                var p = i * 4;
                int a = bgra[p + 3];
                int b = bgra[p], g = bgra[p + 1], r = bgra[p + 2];
                if (a != 255)
                {
                    r = (r * a + 255 * (255 - a) + 127) / 255;
                    g = (g * a + 255 * (255 - a) + 127) / 255;
                    b = (b * a + 255 * (255 - a) + 127) / 255;
                }

                var key = (uint)(r | g << 8 | b << 16);
                if (!hasLast || key != lastKey)
                {
                    float lr = SrgbDecode[r], lg = SrgbDecode[g], lb = SrgbDecode[b];
                    var x = RgbToXyz[0] * lr + RgbToXyz[1] * lg + RgbToXyz[2] * lb;
                    var y = RgbToXyz[3] * lr + RgbToXyz[4] * lg + RgbToXyz[5] * lb;
                    var z = RgbToXyz[6] * lr + RgbToXyz[7] * lg + RgbToXyz[8] * lb;

                    XyzToPcs(lut.Pcs, x, y, z, out input[0], out input[1], out input[2]);
                    lut.Eval(input, ink);

                    lastOut = 0;
                    for (var c = 0; c < 4; c++)
                        lastOut |= (uint)Math.Clamp((int)MathF.Round(ink[c] * 255f), 0, 255) << (c * 8);
                    lastKey = key; hasLast = true;
                }

                res[p] = (byte)lastOut; res[p + 1] = (byte)(lastOut >> 8);
                res[p + 2] = (byte)(lastOut >> 16); res[p + 3] = (byte)(lastOut >> 24);
            }
        });

        return res;
    }
}
