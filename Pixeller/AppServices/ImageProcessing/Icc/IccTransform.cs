using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Pixeller.AppServices.ImageProcessing.Icc;

/// <summary>
/// How the PCS side of a LUT is encoded.
/// </summary>
internal enum PcsEncoding { Xyz, Lab8, LabLegacy16, LabV4 }

/// <summary>
/// One A2B/B2A lookup pipeline (mft1, mft2 or mAB/mBA), evaluated on normalised 0..1 samples.
/// </summary>
internal sealed class IccLut
{
    public int InChannels, OutChannels;
    public PcsEncoding Pcs;

    // Stage order depends on the tag kind; unused stages stay null.
    public float[][]? CurvesA, CurvesM, CurvesB;
    public float[]? Matrix;          // 3x3 + 3 offsets (12 values), applied on the 3-channel PCS side
    public int[] Grid = [];
    public float[] Clut = [];        // OutChannels floats per node, first input dimension varies slowest
    public bool IsMab, IsInput;      // IsInput: device -> PCS (A2B)
    public bool MatrixBeforeClut;    // mft B2A: matrix precedes the input curves

    public static float Sample(float[] curve, float x)
    {
        if (curve.Length == 1) return curve[0];
        var p = Math.Clamp(x, 0f, 1f) * (curve.Length - 1);
        var i = (int)p;
        if (i >= curve.Length - 1) return curve[^1];
        var f = p - i;
        return curve[i] + (curve[i + 1] - curve[i]) * f;
    }

    /// <summary>Multilinear interpolation of the CLUT at the given normalised coordinates.</summary>
    private void Interpolate(Span<float> coords, Span<float> result)
    {
        var n = InChannels;
        Span<int> idx = stackalloc int[4];
        Span<float> frac = stackalloc float[4];
        Span<int> stride = stackalloc int[4];

        var s = OutChannels;
        for (var d = n - 1; d >= 0; d--) { stride[d] = s; s *= Grid[d]; }

        var baseIndex = 0;
        for (var d = 0; d < n; d++)
        {
            var p = Math.Clamp(coords[d], 0f, 1f) * (Grid[d] - 1);
            var i = Math.Min((int)p, Math.Max(Grid[d] - 2, 0));
            idx[d] = i;
            frac[d] = Grid[d] > 1 ? p - i : 0f;
            baseIndex += i * stride[d];
        }

        result.Clear();
        for (var corner = 0; corner < 1 << n; corner++)
        {
            var w = 1f;
            var at = baseIndex;
            for (var d = 0; d < n; d++)
            {
                if ((corner >> d & 1) == 1) { w *= frac[d]; at += stride[d]; }
                else w *= 1f - frac[d];
            }
            if (w == 0f) continue;
            for (var o = 0; o < OutChannels; o++) result[o] += w * Clut[at + o];
        }
    }

    /// <summary>Runs the pipeline. <paramref name="input"/> and <paramref name="output"/> are normalised to 0..1.</summary>
    public void Eval(ReadOnlySpan<float> input, Span<float> output)
    {
        Span<float> v = stackalloc float[4];
        for (var i = 0; i < InChannels; i++) v[i] = input[i];

        if (MatrixBeforeClut && Matrix is not null) ApplyMatrix(v);

        if (IsMab && !IsInput)
        {
            // B2A mBA: B curves -> matrix -> M curves -> CLUT -> A curves
            ApplyCurves(CurvesB, v, InChannels);
            if (Matrix is not null) ApplyMatrix(v);
            ApplyCurves(CurvesM, v, InChannels);
            Span<float> o = stackalloc float[4];
            Interpolate(v, o);
            for (var i = 0; i < OutChannels; i++) output[i] = o[i];
            ApplyCurves(CurvesA, output, OutChannels);
            return;
        }

        // mft1/mft2: input curves -> CLUT -> output curves.  mAB: A curves -> CLUT -> M curves -> matrix -> B curves.
        ApplyCurves(CurvesA, v, InChannels);

        Span<float> res = stackalloc float[4];
        if (Clut.Length > 0) Interpolate(v, res);
        else for (var i = 0; i < OutChannels; i++) res[i] = v[i];

        if (IsMab)
        {
            ApplyCurves(CurvesM, res, OutChannels);
            if (Matrix is not null) ApplyMatrix(res);
            ApplyCurves(CurvesB, res, OutChannels);
        }
        else
            ApplyCurves(CurvesB, res, OutChannels);

        for (var i = 0; i < OutChannels; i++) output[i] = res[i];
    }

    private static void ApplyCurves(float[][]? curves, Span<float> v, int count)
    {
        if (curves is null) return;
        for (var i = 0; i < count && i < curves.Length; i++) v[i] = Sample(curves[i], v[i]);
    }

    private void ApplyMatrix(Span<float> v)
    {
        var m = Matrix!;
        var a = v[0]; var b = v[1]; var c = v[2];
        v[0] = m[0] * a + m[1] * b + m[2] * c + m[9];
        v[1] = m[3] * a + m[4] * b + m[5] * c + m[10];
        v[2] = m[6] * a + m[7] * b + m[8] * c + m[11];
    }
}

/// <summary>
/// Parsed ICC profile exposing only what the CMYK conversions need: header, A2B0 and B2A0.
/// </summary>
internal sealed class IccCmykProfile
{
    public bool IsCmyk, PcsIsLab;
    public IccLut? A2B, B2A;

    // D50 PCS illuminant
    public const double D50X = 0.9642, D50Y = 1.0, D50Z = 0.8249;

    public static IccCmykProfile? TryParse(byte[]? data)
    {
        if (data is null || data.Length < 132) return null;
        try
        {
            if (data[36] != 'a' || data[37] != 'c' || data[38] != 's' || data[39] != 'p') return null;

            var profile = new IccCmykProfile
            {
                IsCmyk = data[16] == 'C' && data[17] == 'M' && data[18] == 'Y' && data[19] == 'K',
                PcsIsLab = data[20] == 'L' && data[21] == 'a' && data[22] == 'b'
            };
            if (!profile.IsCmyk) return null;

            var tagCount = (int)U32(data, 128);
            if (tagCount < 0 || 132 + (long)tagCount * 12 > data.Length) return null;

            var tags = new Dictionary<string, (int off, int size)>();
            for (var i = 0; i < tagCount; i++)
            {
                var p = 132 + i * 12;
                var sig = System.Text.Encoding.ASCII.GetString(data, p, 4);
                var off = (long)U32(data, p + 4); var size = (long)U32(data, p + 8);
                if (off + size <= data.Length && size >= 8) tags[sig] = ((int)off, (int)size);
            }

            var pcs = profile.PcsIsLab ? PcsEncoding.LabLegacy16 : PcsEncoding.Xyz;

            if (tags.TryGetValue("A2B0", out var a) || tags.TryGetValue("A2B1", out a))
                profile.A2B = ParseLut(data, a.off, a.size, 4, 3, pcs, true);
            if (tags.TryGetValue("B2A0", out var b) || tags.TryGetValue("B2A1", out b))
                profile.B2A = ParseLut(data, b.off, b.size, 3, 4, pcs, false);

            return profile.A2B is null && profile.B2A is null ? null : profile;
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentException or OverflowException)
        {
            return null;
        }
    }

    private static uint U32(byte[] d, int p) => BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(p));
    private static int U16(byte[] d, int p) => BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p));
    private static float S15(byte[] d, int p) => BinaryPrimitives.ReadInt32BigEndian(d.AsSpan(p)) / 65536f;

    private static IccLut? ParseLut(byte[] d, int off, int size, int inCh, int outCh, PcsEncoding pcs, bool isInput)
    {
        var type = System.Text.Encoding.ASCII.GetString(d, off, 4);
        // Channels of the profile's device side must match what we expect from the direction.
        return type switch
        {
            "mft1" => ParseMft(d, off, inCh, outCh, pcs, isInput, 1),
            "mft2" => ParseMft(d, off, inCh, outCh, pcs, isInput, 2),
            "mAB " when isInput => ParseMab(d, off, inCh, outCh, pcs, isInput),
            "mBA " when !isInput => ParseMab(d, off, inCh, outCh, pcs, isInput),
            _ => null
        };
    }

    private static IccLut? ParseMft(byte[] d, int off, int inCh, int outCh, PcsEncoding pcs, bool isInput, int bytes)
    {
        if (d[off + 8] != inCh || d[off + 9] != outCh) return null;
        var grid = d[off + 10];
        if (grid < 2) return null;

        var lut = new IccLut
        {
            InChannels = inCh,
            OutChannels = outCh,
            Pcs = bytes == 1 && pcs != PcsEncoding.Xyz ? PcsEncoding.Lab8 : pcs,
            Grid = new int[inCh]
        };
        Array.Fill(lut.Grid, (int)grid);

        var p = off + 12;
        var matrix = new float[12];
        for (var i = 0; i < 9; i++) matrix[i] = S15(d, p + i * 4);
        // The matrix is only meaningful when the input side is XYZ (B2A with an XYZ PCS); skip identity.
        if (!isInput && pcs == PcsEncoding.Xyz && !IsIdentity(matrix))
        {
            lut.Matrix = matrix;
            lut.MatrixBeforeClut = true;
        }
        p += 36;

        int inEntries = 256, outEntries = 256;
        if (bytes == 2)
        {
            inEntries = U16(d, p); outEntries = U16(d, p + 2);
            p += 4;
            if (inEntries < 2 || outEntries < 2 || inEntries > 4096 || outEntries > 4096) return null;
        }

        float Read(ref int at)
        {
            float v;
            if (bytes == 1) { v = d[at] / 255f; at += 1; }
            else { v = U16(d, at) / 65535f; at += 2; }
            return v;
        }

        lut.CurvesA = new float[inCh][];
        for (var c = 0; c < inCh; c++)
        {
            lut.CurvesA[c] = new float[inEntries];
            for (var i = 0; i < inEntries; i++) lut.CurvesA[c][i] = Read(ref p);
        }

        long nodes = 1;
        for (var i = 0; i < inCh; i++) nodes *= grid;
        var total = nodes * outCh;
        if (total > 50_000_000 || p + total * bytes > d.Length) return null;

        lut.Clut = new float[total];
        for (var i = 0; i < total; i++) lut.Clut[i] = Read(ref p);

        lut.CurvesB = new float[outCh][];
        for (var c = 0; c < outCh; c++)
        {
            lut.CurvesB[c] = new float[outEntries];
            for (var i = 0; i < outEntries; i++) lut.CurvesB[c][i] = Read(ref p);
        }

        lut.IsInput = isInput;
        return lut;
    }

    private static bool IsIdentity(float[] m) =>
        Math.Abs(m[0] - 1) < 1e-4 && Math.Abs(m[4] - 1) < 1e-4 && Math.Abs(m[8] - 1) < 1e-4
        && Math.Abs(m[1]) < 1e-4 && Math.Abs(m[2]) < 1e-4 && Math.Abs(m[3]) < 1e-4
        && Math.Abs(m[5]) < 1e-4 && Math.Abs(m[6]) < 1e-4 && Math.Abs(m[7]) < 1e-4;

    private static float[]? ParseCurve(byte[] d, int p, out int length)
    {
        var sig = System.Text.Encoding.ASCII.GetString(d, p, 4);
        if (sig == "curv")
        {
            var n = (int)U32(d, p + 8);
            if (n < 0 || n > 65536 || p + 12 + n * 2 > d.Length) { length = 0; return null; }
            length = 12 + n * 2;
            if (n == 0) return [0f, 1f];
            if (n == 1)
            {
                var g = U16(d, p + 12) / 256f;
                var table = new float[1024];
                for (var i = 0; i < table.Length; i++) table[i] = MathF.Pow(i / (table.Length - 1f), g);
                return table;
            }
            var res = new float[n];
            for (var i = 0; i < n; i++) res[i] = U16(d, p + 12 + i * 2) / 65535f;
            return res;
        }

        if (sig == "para")
        {
            var fn = U16(d, p + 8);
            int[] counts = [1, 3, 4, 5, 7];
            if (fn > 4) { length = 0; return null; }
            var prm = new float[7];
            for (var i = 0; i < counts[fn]; i++) prm[i] = S15(d, p + 12 + i * 4);
            length = 12 + counts[fn] * 4;
            float g = prm[0], a = prm[1], b = prm[2], c = prm[3], dd = prm[4], e = prm[5], f = prm[6];
            var table = new float[1024];
            for (var i = 0; i < table.Length; i++)
            {
                var x = i / (table.Length - 1f);
                float y;
                switch (fn)
                {
                    case 0: y = MathF.Pow(x, g); break;
                    case 1: y = x >= -b / a ? MathF.Pow(a * x + b, g) : 0f; break;
                    case 2: y = x >= -b / a ? MathF.Pow(a * x + b, g) + c : c; break;
                    case 3: y = x >= dd ? MathF.Pow(a * x + b, g) : c * x; break;
                    default: y = x >= dd ? MathF.Pow(a * x + b, g) + e : c * x + f; break;
                }
                table[i] = Math.Clamp(float.IsNaN(y) ? 0f : y, 0f, 1f);
            }
            return table;
        }

        length = 0;
        return null;
    }

    private static float[][]? ParseCurveSet(byte[] d, int p, int count)
    {
        var res = new float[count][];
        for (var i = 0; i < count; i++)
        {
            var c = ParseCurve(d, p, out var len);
            if (c is null) return null;
            res[i] = c;
            p += (len + 3) & ~3;
        }
        return res;
    }

    private static IccLut? ParseMab(byte[] d, int off, int inCh, int outCh, PcsEncoding pcs, bool isInput)
    {
        if (d[off + 8] != inCh || d[off + 9] != outCh) return null;

        int OffsetAt(int rel) => (int)U32(d, off + rel);
        var offB = OffsetAt(12); var offMatrix = OffsetAt(16); var offM = OffsetAt(20);
        var offClut = OffsetAt(24); var offA = OffsetAt(28);

        // mAB: device is the input (A side, inCh) and PCS the output (B side, 3 ch); mBA mirrors it.
        var lut = new IccLut
        {
            InChannels = inCh,
            OutChannels = outCh,
            IsMab = true,
            IsInput = isInput,
            Pcs = pcs == PcsEncoding.Xyz ? PcsEncoding.Xyz : PcsEncoding.LabV4
        };

        var pcsCh = 3;
        if (offB != 0) lut.CurvesB = ParseCurveSet(d, off + offB, pcsCh);
        if (offM != 0) lut.CurvesM = ParseCurveSet(d, off + offM, pcsCh);
        if (offA != 0) lut.CurvesA = ParseCurveSet(d, off + offA, isInput ? inCh : outCh);

        if (offMatrix != 0)
        {
            var m = new float[12];
            for (var i = 0; i < 12; i++) m[i] = S15(d, off + offMatrix + i * 4);
            lut.Matrix = m;
        }

        if (offClut != 0)
        {
            var p = off + offClut;
            var cin = isInput ? inCh : 3;
            lut.Grid = new int[cin];
            long nodes = 1;
            for (var i = 0; i < cin; i++) { lut.Grid[i] = d[p + i]; nodes *= lut.Grid[i]; if (lut.Grid[i] < 2) return null; }
            var precision = d[p + 16];
            var total = nodes * outCh;
            if (precision is not (1 or 2) || total > 50_000_000 || p + 20 + total * precision > d.Length) return null;
            p += 20;
            lut.Clut = new float[total];
            for (var i = 0; i < total; i++)
            {
                lut.Clut[i] = precision == 1 ? d[p] / 255f : U16(d, p) / 65535f;
                p += precision;
            }
        }
        else if (inCh != outCh)
            return null;

        // For mBA the CLUT maps 3 -> 4, so the logical input of the pipeline has 3 channels.
        if (!isInput) { lut.InChannels = 3; lut.OutChannels = 4; }
        return lut;
    }
}
