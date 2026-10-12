using System;
using System.Collections.Generic;
using System.IO;

namespace PixelArtEditor.AppServices.ImageProcessing.Formats;

/// <summary>
/// Small baseline JPEG codec for 4-component (CMYK / YCCK) images. ImageSharp only exposes CMYK JPEGs as naive RGB,
/// which makes ICC-based conversion impossible, so raw ink planes are decoded and encoded here.
/// Decoding supports sequential Huffman (SOF0/SOF1, 8 bit) with subsampling and restart intervals;
/// progressive files return null so the caller can fall back to ImageSharp.
/// </summary>
internal static class JpegCmykCodec
{
    private static readonly int[] ZigZag =
    [
        0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51, 58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63
    ];

    public sealed class Decoded
    {
        public int Width, Height;
        public byte[] Cmyk = [];      // interleaved ink coverage, 255 = full ink
        public byte[]? Icc;
    }

    // ------------------------------------------------------------------ decoding

    private sealed class Component
    {
        public int Id, H, V, Tq, Td, Ta, Pred;
        public int BlocksW, BlocksH;
        public short[] Coeffs = [];
        public byte[] Plane = [];
        public int PlaneW, PlaneH;
    }

    private sealed class Huffman
    {
        public readonly int[] MinCode = new int[17], MaxCode = new int[18], ValPtr = new int[17];
        public byte[] Values = [];

        public Huffman(byte[] counts, byte[] values)
        {
            Values = values;
            var code = 0; var k = 0;
            for (var l = 1; l <= 16; l++)
            {
                ValPtr[l] = k;
                MinCode[l] = code;
                code += counts[l - 1];
                k += counts[l - 1];
                MaxCode[l] = counts[l - 1] == 0 ? -1 : code - 1;
                code <<= 1;
            }
            MaxCode[17] = int.MaxValue;
        }
    }

    private sealed class BitReader(byte[] data, int pos)
    {
        public int Pos = pos;
        private int _bits, _count;
        public bool Marker;

        public int Bit()
        {
            if (_count == 0)
            {
                if (Pos >= data.Length) throw new InvalidDataException();
                var b = data[Pos++];
                if (b == 0xFF)
                {
                    var n = Pos < data.Length ? data[Pos] : 0;
                    if (n == 0) Pos++;
                    else { Pos--; Marker = true; b = 0; }
                }
                _bits = b; _count = 8;
            }
            _count--;
            return _bits >> _count & 1;
        }

        public int Receive(int n)
        {
            var v = 0;
            for (var i = 0; i < n; i++) v = v << 1 | Bit();
            return v;
        }

        public void Reset() { _count = 0; Marker = false; }
    }

    private static int Decode(BitReader r, Huffman h)
    {
        var code = 0;
        for (var l = 1; l <= 16; l++)
        {
            code = code << 1 | r.Bit();
            if (h.MaxCode[l] >= 0 && code <= h.MaxCode[l] && code >= h.MinCode[l])
                return h.Values[h.ValPtr[l] + code - h.MinCode[l]];
        }
        throw new InvalidDataException();
    }

    private static int Extend(int v, int t) => t == 0 ? 0 : v < 1 << (t - 1) ? v - (1 << t) + 1 : v;

    public static Decoded? TryDecode(byte[] d)
    {
        try { return DecodeCore(d); }
        catch (Exception ex) when (ex is InvalidDataException or IndexOutOfRangeException or ArgumentException or OverflowException)
        {
            return null;
        }
    }

    private static Decoded? DecodeCore(byte[] d)
    {
        if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8) return null;

        var qt = new int[4][];
        var dcTables = new Huffman?[4];
        var acTables = new Huffman?[4];
        var comps = new List<Component>();
        int width = 0, height = 0, restart = 0, adobeTransform = -1;
        var adobe = false;
        var iccParts = new SortedDictionary<int, byte[]>();
        var p = 2;

        while (p + 4 <= d.Length)
        {
            if (d[p] != 0xFF) { p++; continue; }
            var marker = d[p + 1];
            if (marker == 0xFF) { p++; continue; }
            p += 2;
            if (marker is 0x01 or (>= 0xD0 and <= 0xD8)) continue;
            if (marker == 0xD9) break;

            var len = d[p] << 8 | d[p + 1];
            var seg = p + 2; var end = p + len;
            if (len < 2 || end > d.Length) return null;

            switch (marker)
            {
                case 0xC0 or 0xC1:
                    if (d[seg] != 8) return null;
                    height = d[seg + 1] << 8 | d[seg + 2];
                    width = d[seg + 3] << 8 | d[seg + 4];
                    var n = d[seg + 5];
                    if (n != 4 || width <= 0 || height <= 0 || (long)width * height > 100_000_000) return null;
                    comps.Clear();
                    for (var i = 0; i < n; i++)
                    {
                        var o = seg + 6 + i * 3;
                        var c = new Component { Id = d[o], H = d[o + 1] >> 4, V = d[o + 1] & 15, Tq = d[o + 2] };
                        if (c.H is < 1 or > 4 || c.V is < 1 or > 4 || c.Tq > 3) return null;
                        comps.Add(c);
                    }
                    break;
                case 0xC2 or 0xC3 or (>= 0xC5 and <= 0xC7) or (>= 0xC9 and <= 0xCB) or (>= 0xCD and <= 0xCF):
                    return null;   // progressive, lossless, arithmetic: not handled
                case 0xC4:
                    for (var o = seg; o < end;)
                    {
                        var tc = d[o] >> 4; var th = d[o] & 15;
                        var counts = d[(o + 1)..(o + 17)];
                        var total = 0;
                        foreach (var c in counts) total += c;
                        var vals = d[(o + 17)..(o + 17 + total)];
                        if (th > 3) return null;
                        (tc == 0 ? dcTables : acTables)[th] = new Huffman(counts, vals);
                        o += 17 + total;
                    }
                    break;
                case 0xDB:
                    for (var o = seg; o < end;)
                    {
                        var pq = d[o] >> 4; var tq = d[o] & 15;
                        if (tq > 3) return null;
                        var table = new int[64];
                        for (var i = 0; i < 64; i++)
                            table[ZigZag[i]] = pq == 0 ? d[o + 1 + i] : d[o + 1 + i * 2] << 8 | d[o + 2 + i * 2];
                        qt[tq] = table;
                        o += 1 + 64 * (pq + 1);
                    }
                    break;
                case 0xDD:
                    restart = d[seg] << 8 | d[seg + 1];
                    break;
                case 0xEE when len >= 14 && d[seg] == 'A' && d[seg + 1] == 'd' && d[seg + 2] == 'o' && d[seg + 3] == 'b' && d[seg + 4] == 'e':
                    adobe = true;
                    adobeTransform = d[seg + 11];
                    break;
                case 0xE2 when len > 16 && System.Text.Encoding.ASCII.GetString(d, seg, 11) == "ICC_PROFILE":
                    iccParts[d[seg + 12]] = d[(seg + 14)..end];
                    break;
                case 0xDA:
                    if (comps.Count != 4) return null;
                    var ns = d[seg];
                    if (ns != 4) return null;    // non-interleaved scans are not supported
                    for (var i = 0; i < ns; i++)
                    {
                        var cid = d[seg + 1 + i * 2]; var tt = d[seg + 2 + i * 2];
                        var comp = comps.Find(c => c.Id == cid) ?? throw new InvalidDataException();
                        comp.Td = tt >> 4; comp.Ta = tt & 15;
                    }
                    DecodeScan(d, end, comps, width, height, restart, dcTables, acTables);
                    return Finish(comps, qt, width, height, adobe, adobeTransform, iccParts);
            }

            p = end;
        }

        return null;
    }

    private static void DecodeScan(byte[] d, int pos, List<Component> comps, int width, int height, int restart,
        Huffman?[] dcTables, Huffman?[] acTables)
    {
        int hMax = 1, vMax = 1;
        foreach (var c in comps) { hMax = Math.Max(hMax, c.H); vMax = Math.Max(vMax, c.V); }

        var mcusX = (width + 8 * hMax - 1) / (8 * hMax);
        var mcusY = (height + 8 * vMax - 1) / (8 * vMax);

        foreach (var c in comps)
        {
            c.BlocksW = mcusX * c.H; c.BlocksH = mcusY * c.V;
            c.Coeffs = new short[checked(c.BlocksW * c.BlocksH * 64)];
            c.Pred = 0;
        }

        var r = new BitReader(d, pos);
        var mcu = 0;
        for (var my = 0; my < mcusY; my++)
            for (var mx = 0; mx < mcusX; mx++, mcu++)
            {
                if (restart > 0 && mcu > 0 && mcu % restart == 0)
                {
                    r.Reset();
                    // skip to and past the RSTn marker
                    while (r.Pos + 1 < d.Length && !(d[r.Pos] == 0xFF && d[r.Pos + 1] is >= 0xD0 and <= 0xD7)) r.Pos++;
                    r.Pos += 2;
                    foreach (var c in comps) c.Pred = 0;
                }

                foreach (var c in comps)
                {
                    var dc = dcTables[c.Td] ?? throw new InvalidDataException();
                    var ac = acTables[c.Ta] ?? throw new InvalidDataException();

                    for (var by = 0; by < c.V; by++)
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            var block = ((my * c.V + by) * c.BlocksW + mx * c.H + bx) * 64;
                            var t = Decode(r, dc);
                            c.Pred += Extend(r.Receive(t), t);
                            c.Coeffs[block] = (short)c.Pred;

                            for (var k = 1; k < 64;)
                            {
                                var rs = Decode(r, ac);
                                var s = rs & 15; var run = rs >> 4;
                                if (s == 0)
                                {
                                    if (run == 15) { k += 16; continue; }
                                    break;
                                }
                                k += run;
                                if (k > 63) throw new InvalidDataException();
                                c.Coeffs[block + ZigZag[k]] = (short)Extend(r.Receive(s), s);
                                k++;
                            }
                        }
                }
            }
    }

    private static Decoded Finish(List<Component> comps, int[][] qt, int width, int height, bool adobe, int transform,
        SortedDictionary<int, byte[]> iccParts)
    {
        int hMax = 1, vMax = 1;
        foreach (var c in comps) { hMax = Math.Max(hMax, c.H); vMax = Math.Max(vMax, c.V); }

        foreach (var c in comps)
        {
            var table = qt[c.Tq] ?? throw new InvalidDataException();
            c.PlaneW = c.BlocksW * 8; c.PlaneH = c.BlocksH * 8;
            c.Plane = new byte[c.PlaneW * c.PlaneH];
            System.Threading.Tasks.Parallel.For(0, c.BlocksH, by =>
            {
                Span<float> tmp = stackalloc float[64];
                for (var bx = 0; bx < c.BlocksW; bx++)
                {
                    var block = (by * c.BlocksW + bx) * 64;
                    for (var i = 0; i < 64; i++) tmp[i] = c.Coeffs[block + i] * table[i];
                    Idct(tmp);
                    for (var y = 0; y < 8; y++)
                        for (var x = 0; x < 8; x++)
                            c.Plane[(by * 8 + y) * c.PlaneW + bx * 8 + x] =
                                (byte)Math.Clamp((int)MathF.Round(tmp[y * 8 + x]) + 128, 0, 255);
                }
            });
            c.Coeffs = [];
        }

        // Adobe transform 2 = YCCK. Without an Adobe marker components are plain CMYK.
        var ycck = adobe && transform == 2;
        // Adobe stores inverted ink values (255 = no ink).
        var inverted = adobe;

        var res = new byte[checked(width * height * 4)];
        var cs = comps;
        System.Threading.Tasks.Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var o = (y * width + x) * 4;
                var s0 = Sample(cs[0], x, y, hMax, vMax);
                var s1 = Sample(cs[1], x, y, hMax, vMax);
                var s2 = Sample(cs[2], x, y, hMax, vMax);
                var s3 = Sample(cs[3], x, y, hMax, vMax);

                if (ycck)
                {
                    float Y = s0, cb = s1 - 128f, cr = s2 - 128f;
                    s0 = ClampB(Y + 1.402f * cr);
                    s1 = ClampB(Y - 0.344136f * cb - 0.714136f * cr);
                    s2 = ClampB(Y + 1.772f * cb);
                }

                res[o] = (byte)(inverted ? 255 - s0 : s0);
                res[o + 1] = (byte)(inverted ? 255 - s1 : s1);
                res[o + 2] = (byte)(inverted ? 255 - s2 : s2);
                res[o + 3] = (byte)(inverted ? 255 - s3 : s3);
            }
        });

        byte[]? icc = null;
        if (iccParts.Count > 0)
        {
            using var ms = new MemoryStream();
            foreach (var part in iccParts.Values) ms.Write(part);
            icc = ms.ToArray();
        }

        return new Decoded { Width = width, Height = height, Cmyk = res, Icc = icc };
    }

    private static int ClampB(float v) => Math.Clamp((int)MathF.Round(v), 0, 255);

    private static int Sample(Component c, int x, int y, int hMax, int vMax)
    {
        var sx = Math.Min(x * c.H / hMax, c.PlaneW - 1);
        var sy = Math.Min(y * c.V / vMax, c.PlaneH - 1);
        return c.Plane[sy * c.PlaneW + sx];
    }

    // Separable float IDCT (AAN-free, straightforward and accurate).
    private static readonly float[] CosTable = BuildCos();

    private static float[] BuildCos()
    {
        var t = new float[64];
        for (var x = 0; x < 8; x++)
            for (var u = 0; u < 8; u++)
                t[x * 8 + u] = (float)((u == 0 ? 1 / Math.Sqrt(2) : 1) * Math.Cos((2 * x + 1) * u * Math.PI / 16) / 2);
        return t;
    }

    private static void Idct(Span<float> block)
    {
        Span<float> tmp = stackalloc float[64];
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                float s = 0;
                for (var u = 0; u < 8; u++) s += CosTable[x * 8 + u] * block[y * 8 + u];
                tmp[y * 8 + x] = s;
            }
        for (var x = 0; x < 8; x++)
            for (var y = 0; y < 8; y++)
            {
                float s = 0;
                for (var v = 0; v < 8; v++) s += CosTable[y * 8 + v] * tmp[v * 8 + x];
                block[y * 8 + x] = s;
            }
    }

    // ------------------------------------------------------------------ encoding

    private static readonly int[] StdLuma =
    [
        16, 11, 10, 16, 24, 40, 51, 61, 12, 12, 14, 19, 26, 58, 60, 55, 14, 13, 16, 24, 40, 57, 69, 56, 14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77, 24, 35, 55, 64, 81, 104, 113, 92, 49, 64, 78, 87, 103, 121, 120, 101, 72, 92, 95, 98, 112, 100, 103, 99
    ];

    private static readonly byte[] DcCounts = [0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] DcValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

    private static readonly byte[] AcCounts = [0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d];
    private static readonly byte[] AcValues =
    [
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xa1, 0x08,
        0x23, 0x42, 0xb1, 0xc1, 0x15, 0x52, 0xd1, 0xf0, 0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0a, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2a, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
        0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6,
        0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xe1, 0xe2,
        0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa
    ];

    private static (int[] code, int[] size) BuildEncoder(byte[] counts, byte[] values)
    {
        var code = new int[256]; var size = new int[256];
        var c = 0; var k = 0;
        for (var l = 1; l <= 16; l++)
        {
            for (var i = 0; i < counts[l - 1]; i++, k++) { code[values[k]] = c++; size[values[k]] = l; }
            c <<= 1;
        }
        return (code, size);
    }

    private sealed class BitWriter(Stream s)
    {
        private int _acc, _count;

        public void Write(int code, int size)
        {
            for (var i = size - 1; i >= 0; i--)
            {
                _acc = _acc << 1 | code >> i & 1;
                if (++_count == 8) Flush();
            }
        }

        private void Flush()
        {
            s.WriteByte((byte)_acc);
            if (_acc == 0xFF) s.WriteByte(0);
            _acc = 0; _count = 0;
        }

        public void Finish()
        {
            while (_count != 0) Write(1, 1);
        }
    }

    /// <summary>
    /// Encodes interleaved CMYK ink data (255 = full ink) as an Adobe-style CMYK JPEG (no subsampling,
    /// inverted samples, Adobe transform 0) and embeds <paramref name="icc"/> in APP2 chunks.
    /// </summary>
    public static void Encode(Stream stream, byte[] cmyk, int width, int height, int quality, byte[]? icc)
    {
        quality = Math.Clamp(quality, 1, 100);
        var scale = quality < 50 ? 5000 / quality : 200 - quality * 2;
        var q = new int[64];       // natural order
        var qz = new int[64];      // zig-zag order
        for (var i = 0; i < 64; i++) q[i] = Math.Clamp((StdLuma[i] * scale + 50) / 100, 1, 255);
        for (var i = 0; i < 64; i++) qz[i] = q[ZigZag[i]];

        var (dcCode, dcSize) = BuildEncoder(DcCounts, DcValues);
        var (acCode, acSize) = BuildEncoder(AcCounts, AcValues);

        var w = new BinaryWriter(stream);
        void Marker(byte m) { w.Write((byte)0xFF); w.Write(m); }
        void Len(int n) { w.Write((byte)(n >> 8)); w.Write((byte)n); }

        Marker(0xD8);

        // JFIF-less: Adobe APP14 marks CMYK
        Marker(0xEE); Len(14);
        w.Write("Adobe"u8.ToArray()); w.Write((byte)0); w.Write((byte)100); w.Write((short)0); w.Write((short)0); w.Write((byte)0);

        if (icc is { Length: > 0 })
        {
            const int chunk = 65519 - 14;
            var parts = (icc.Length + chunk - 1) / chunk;
            for (var i = 0; i < parts; i++)
            {
                var n = Math.Min(chunk, icc.Length - i * chunk);
                Marker(0xE2); Len(2 + 14 + n);
                w.Write("ICC_PROFILE\0"u8.ToArray()); w.Write((byte)(i + 1)); w.Write((byte)parts);
                w.Write(icc, i * chunk, n);
            }
        }

        Marker(0xDB); Len(2 + 65); w.Write((byte)0); for (var i = 0; i < 64; i++) w.Write((byte)qz[i]);

        Marker(0xC0); Len(8 + 4 * 3);
        w.Write((byte)8); w.Write((byte)(height >> 8)); w.Write((byte)height); w.Write((byte)(width >> 8)); w.Write((byte)width);
        w.Write((byte)4);
        for (byte i = 0; i < 4; i++) { w.Write((byte)(i + 1)); w.Write((byte)0x11); w.Write((byte)0); }

        Marker(0xC4); Len(2 + 17 + DcValues.Length); w.Write((byte)0x00); w.Write(DcCounts); w.Write(DcValues);
        Marker(0xC4); Len(2 + 17 + AcValues.Length); w.Write((byte)0x10); w.Write(AcCounts); w.Write(AcValues);

        Marker(0xDA); Len(6 + 2 * 4);
        w.Write((byte)4);
        for (byte i = 0; i < 4; i++) { w.Write((byte)(i + 1)); w.Write((byte)0x00); }
        w.Write((byte)0); w.Write((byte)63); w.Write((byte)0);
        w.Flush();

        var bw = new BitWriter(stream);
        var pred = new int[4];
        var block = new float[64];
        var coef = new int[64];

        for (var by = 0; by < height; by += 8)
            for (var bx = 0; bx < width; bx += 8)
                for (var c = 0; c < 4; c++)
                {
                    for (var y = 0; y < 8; y++)
                        for (var x = 0; x < 8; x++)
                        {
                            var sx = Math.Min(bx + x, width - 1); var sy = Math.Min(by + y, height - 1);
                            // Adobe convention: store 255 - ink
                            block[y * 8 + x] = 255 - cmyk[(sy * width + sx) * 4 + c] - 128;
                        }

                    Fdct(block, coef, q);

                    var diff = coef[0] - pred[c];
                    pred[c] = coef[0];
                    EmitValue(bw, dcCode, dcSize, 0, diff);

                    var run = 0;
                    for (var k = 1; k < 64; k++)
                    {
                        var v = coef[ZigZag[k]];
                        if (v == 0) { run++; continue; }
                        while (run > 15) { bw.Write(acCode[0xF0], acSize[0xF0]); run -= 16; }
                        EmitValue(bw, acCode, acSize, run << 4, v);
                        run = 0;
                    }
                    if (run > 0) bw.Write(acCode[0x00], acSize[0x00]);
                }

        bw.Finish();
        Marker(0xD9);
        w.Flush();
    }

    private static void EmitValue(BitWriter bw, int[] code, int[] size, int prefix, int v)
    {
        var abs = Math.Abs(v);
        var bits = 0;
        while (abs >> bits != 0) bits++;
        var sym = prefix | bits;
        bw.Write(code[sym], size[sym]);
        if (bits > 0) bw.Write(v < 0 ? v - 1 & (1 << bits) - 1 : v, bits);
    }

    private static void Fdct(float[] block, int[] coef, int[] q)
    {
        var tmp = new float[64];
        for (var y = 0; y < 8; y++)
            for (var u = 0; u < 8; u++)
            {
                float s = 0;
                for (var x = 0; x < 8; x++) s += CosTable[x * 8 + u] * block[y * 8 + x];
                tmp[y * 8 + u] = s;
            }
        for (var u = 0; u < 8; u++)
            for (var v = 0; v < 8; v++)
            {
                float s = 0;
                for (var y = 0; y < 8; y++) s += CosTable[y * 8 + v] * tmp[y * 8 + u];
                coef[v * 8 + u] = (int)MathF.Round(s / q[v * 8 + u]);
            }
    }
}
