using PixelArtEditor.Models.Canvas;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PixelArtEditor.AppServices.ImageProcessing.Formats;

/// <summary>
/// Reads and writes CMYK (PhotometricInterpretation = Separated) TIFF files, 8 and 16 bits per sample,
/// with None / LZW / Deflate / PackBits strip compression.
/// </summary>
public static class TiffCmykService
{
    private const int MaxPixels = 100_000_000;

    private const ushort TagWidth = 256, TagHeight = 257, TagBits = 258, TagCompression = 259, TagPhotometric = 262,
        TagStripOffsets = 273, TagSamples = 277, TagRowsPerStrip = 278, TagStripCounts = 279,
        TagXRes = 282, TagYRes = 283, TagPlanar = 284, TagResUnit = 296, TagPredictor = 317,
        TagTileWidth = 322, TagInkSet = 332, TagIcc = 34675;

    private sealed class Ifd
    {
        public Dictionary<ushort, (int type, int count, long valueOffset)> Entries = [];
        public bool Little;
        public byte[] Data = [];

        public bool Has(ushort tag) => Entries.ContainsKey(tag);

        public int[] Values(ushort tag)
        {
            if (!Entries.TryGetValue(tag, out var e)) return [];
            var size = e.type switch { 3 => 2, 4 => 4, _ => 1 };
            var res = new int[e.count];
            for (var i = 0; i < e.count; i++)
            {
                var p = (int)(e.valueOffset + (long)i * size);
                res[i] = size switch
                {
                    2 => U16(p),
                    4 => (int)U32(p),
                    _ => Data[p]
                };
            }
            return res;
        }

        public int Value(ushort tag, int def)
        {
            var v = Values(tag);
            return v.Length > 0 ? v[0] : def;
        }

        public byte[]? Bytes(ushort tag)
        {
            if (!Entries.TryGetValue(tag, out var e)) return null;
            if (e.valueOffset < 0 || e.valueOffset + e.count > Data.Length) return null;
            return Data.AsSpan((int)e.valueOffset, e.count).ToArray();
        }

        public double Rational(ushort tag, double def)
        {
            if (!Entries.TryGetValue(tag, out var e) || e.valueOffset + 8 > Data.Length) return def;
            var p = (int)e.valueOffset;
            var den = U32(p + 4);
            return den == 0 ? def : (double)U32(p) / den;
        }

        public int U16(int p) => Little
            ? BinaryPrimitives.ReadUInt16LittleEndian(Data.AsSpan(p))
            : BinaryPrimitives.ReadUInt16BigEndian(Data.AsSpan(p));

        public uint U32(int p) => Little
            ? BinaryPrimitives.ReadUInt32LittleEndian(Data.AsSpan(p))
            : BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(p));
    }

    private static Ifd? ReadIfd(byte[] data)
    {
        if (data.Length < 8) return null;

        bool little;
        if (data[0] == 'I' && data[1] == 'I') little = true;
        else if (data[0] == 'M' && data[1] == 'M') little = false;
        else return null;

        var ifd = new Ifd { Little = little, Data = data };
        if (ifd.U16(2) != 42) return null;

        var p = (long)ifd.U32(4);
        if (p < 8 || p + 2 > data.Length) return null;

        var count = ifd.U16((int)p);
        p += 2;
        if (p + (long)count * 12 > data.Length) return null;

        for (var i = 0; i < count; i++, p += 12)
        {
            var pos = (int)p;
            var tag = (ushort)ifd.U16(pos);
            var type = ifd.U16(pos + 2);
            var n = (int)ifd.U32(pos + 4);
            var size = type switch { 3 => 2, 4 => 4, 5 => 8, _ => 1 };
            var total = (long)n * size;
            long offset = total <= 4 ? pos + 8 : ifd.U32(pos + 8);

            if (n < 0 || offset + total > data.Length) continue;
            ifd.Entries[tag] = (type, n, offset);
        }

        return ifd;
    }

    /// <summary>
    /// True when the stream holds a TIFF this service can decode (CMYK, chunky, stripped).
    /// </summary>
    public static bool IsCmyk(Stream stream)
    {
        var pos = stream.Position;
        try
        {
            var ifd = ReadIfd(ReadAll(stream));
            return ifd is not null && IsSupportedCmyk(ifd);
        }
        finally { stream.Position = pos; }
    }

    private static bool IsSupportedCmyk(Ifd ifd)
    {
        var bits = ifd.Values(TagBits);
        return ifd.Value(TagPhotometric, -1) == 5
            && ifd.Value(TagSamples, 1) >= 4
            && ifd.Value(TagInkSet, 1) == 1
            && ifd.Value(TagPlanar, 1) == 1
            && !ifd.Has(TagTileWidth)
            && bits.Length >= 4 && bits.Take(4).All(b => b == bits[0]) && bits[0] is 8 or 16;
    }

    private static byte[] ReadAll(Stream stream)
    {
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    public static (PixelModel? model, string? error) Load(Stream stream)
    {
        try
        {
            var data = ReadAll(stream);
            var ifd = ReadIfd(data);
            if (ifd is null || !IsSupportedCmyk(ifd))
                return (null, LocalizationService.Get("UnsupportedImage"));

            var width = ifd.Value(TagWidth, 0);
            var height = ifd.Value(TagHeight, 0);
            if (width <= 0 || height <= 0 || (long)width * height > MaxPixels)
                return (null, LocalizationService.Get("FileCorrupted"));

            var spp = ifd.Value(TagSamples, 4);
            var bytesPerSample = ifd.Values(TagBits)[0] / 8;
            var compression = ifd.Value(TagCompression, 1);
            var predictor = ifd.Value(TagPredictor, 1);
            var rowsPerStrip = Math.Clamp(ifd.Value(TagRowsPerStrip, height), 1, height);
            var offsets = ifd.Values(TagStripOffsets);
            var counts = ifd.Values(TagStripCounts);
            var rowBytes = checked(width * spp * bytesPerSample);
            var stripCount = (height + rowsPerStrip - 1) / rowsPerStrip;

            if (offsets.Length < stripCount || (counts.Length < stripCount && compression != 1))
                return (null, LocalizationService.Get("FileCorrupted"));

            var raw = new byte[checked(rowBytes * height)];

            for (var s = 0; s < stripCount; s++)
            {
                var rows = Math.Min(rowsPerStrip, height - s * rowsPerStrip);
                var expected = rows * rowBytes;
                var start = (uint)offsets[s];
                var len = counts.Length > s ? (uint)counts[s] : (uint)Math.Min(expected, data.Length - start);
                if (start > data.Length || start + len > data.Length)
                    return (null, LocalizationService.Get("FileCorrupted"));

                var chunk = data.AsSpan((int)start, (int)len);
                byte[] strip;

                switch (compression)
                {
                    case 1:
                        strip = chunk.ToArray();
                        break;
                    case 5:
                        strip = TiffCodecs.LzwDecode(chunk, expected);
                        break;
                    case 8 or 32946:
                        strip = TiffCodecs.DeflateDecode(chunk, expected);
                        break;
                    case 32773:
                        strip = TiffCodecs.PackBitsDecode(chunk, expected);
                        break;
                    default:
                        return (null, LocalizationService.Get("TiffCompressionUnsupported"));
                }

                if (strip.Length < expected) Array.Resize(ref strip, expected);
                if (predictor == 2) Unpredict(strip, rows, width, spp, bytesPerSample, ifd.Little);

                Buffer.BlockCopy(strip, 0, raw, s * rowsPerStrip * rowBytes, expected);
            }

            var icc = ifd.Bytes(TagIcc);
            var bgra = RawToBgra(raw, width * height, spp, bytesPerSample, ifd.Little, icc);

            var dpiX = 96f; var dpiY = 96f;
            var unit = ifd.Value(TagResUnit, 2);
            var xr = ifd.Rational(TagXRes, 0); var yr = ifd.Rational(TagYRes, 0);
            if (unit is 2 or 3 && xr > 0 && yr > 0)
            {
                var scale = unit == 3 ? 2.54 : 1.0;
                dpiX = (float)(xr * scale); dpiY = (float)(yr * scale);
            }

            var model = new PixelModel
            {
                Width = width,
                Height = height,
                ColorMode = ColorMode.CMYK,
                BitDepth = bytesPerSample == 2 ? BitDepth.Bit16 : BitDepth.Bit8,
                AlphaFormat = AlphaFormat.None,
                ColorSpace = ColorSpace.Cmyk,
                SrcColorSpace = ColorSpace.Cmyk,
                DpiX = dpiX,
                DpiY = dpiY,
                Data = bgra,
                IccProfile = icc,
                Compression = compression switch
                {
                    5 => TiffCompressionMode.Lzw,
                    8 or 32946 => TiffCompressionMode.Deflate,
                    32773 => TiffCompressionMode.PackBits,
                    _ => TiffCompressionMode.None
                }
            };

            return (model, null);
        }
        catch (Exception ex) when (ex is InvalidDataException or OverflowException or ArgumentException or IndexOutOfRangeException)
        {
            return (null, LocalizationService.Get("FileCorrupted"));
        }
    }

    // --- decoding helpers ---

    private static void Unpredict(byte[] strip, int rows, int width, int spp, int bytesPerSample, bool little)
    {
        var rowBytes = width * spp * bytesPerSample;
        for (var r = 0; r < rows; r++)
        {
            var row = strip.AsSpan(r * rowBytes, rowBytes);
            if (bytesPerSample == 1)
            {
                for (var i = spp; i < row.Length; i++) row[i] += row[i - spp];
            }
            else
            {
                for (var i = spp; i < width * spp; i++)
                {
                    var cur = i * 2; var prev = (i - spp) * 2;
                    var sum = little
                        ? BinaryPrimitives.ReadUInt16LittleEndian(row[cur..]) + BinaryPrimitives.ReadUInt16LittleEndian(row[prev..])
                        : BinaryPrimitives.ReadUInt16BigEndian(row[cur..]) + BinaryPrimitives.ReadUInt16BigEndian(row[prev..]);
                    if (little) BinaryPrimitives.WriteUInt16LittleEndian(row[cur..], (ushort)sum);
                    else BinaryPrimitives.WriteUInt16BigEndian(row[cur..], (ushort)sum);
                }
            }
        }
    }

    private static byte[] RawToBgra(byte[] raw, int pixels, int spp, int bytesPerSample, bool little, byte[]? icc)
    {
        var cmyk = new byte[pixels * 4];
        System.Threading.Tasks.Parallel.For(0, (pixels + 4095) / 4096, c =>
        {
            var end = Math.Min(pixels, (c + 1) * 4096);
            for (var i = c * 4096; i < end; i++)
            {
                var p = i * spp * bytesPerSample;
                var o = i * 4;
                if (bytesPerSample == 1)
                {
                    cmyk[o] = raw[p]; cmyk[o + 1] = raw[p + 1]; cmyk[o + 2] = raw[p + 2]; cmyk[o + 3] = raw[p + 3];
                }
                else
                {
                    // 16-bit: scale down to 8 bits (rounded)
                    cmyk[o] = (byte)Sample16(raw, p, little); cmyk[o + 1] = (byte)Sample16(raw, p + 2, little);
                    cmyk[o + 2] = (byte)Sample16(raw, p + 4, little); cmyk[o + 3] = (byte)Sample16(raw, p + 6, little);
                }
            }
        });

        return CmykConverter.ToBgra(cmyk, icc);
    }

    private static int Sample16(byte[] d, int p, bool little)
    {
        var v = little ? BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(p)) : BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p));
        return (v * 255 + 32767) / 65535;
    }

    // --- writing ---

    public static void Save(Stream stream, byte[] bgra, int width, int height, float dpiX, float dpiY,
        BitDepth bitDepth, TiffCompressionMode compression, byte[]? icc)
    {
        if (width <= 0 || height <= 0 || bgra.Length != checked(width * height * 4))
            throw new InvalidOperationException(LocalizationService.Get("FailedExport"));

        var cmyk = CmykConverter.FromBgra(bgra, icc);
        var sixteen = bitDepth == BitDepth.Bit16;
        var bytesPerSample = sixteen ? 2 : 1;
        var rowBytes = width * 4 * bytesPerSample;
        var rowsPerStrip = Math.Clamp(65536 / Math.Max(1, rowBytes), 1, height);
        var stripCount = (height + rowsPerStrip - 1) / rowsPerStrip;
        var predictor = !sixteen && compression is TiffCompressionMode.Lzw or TiffCompressionMode.Deflate;

        using var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write((byte)'I'); w.Write((byte)'I'); w.Write((ushort)42); w.Write(0u);

        var offsets = new uint[stripCount];
        var sizes = new uint[stripCount];

        for (var s = 0; s < stripCount; s++)
        {
            var rows = Math.Min(rowsPerStrip, height - s * rowsPerStrip);
            var raw = new byte[rows * rowBytes];

            for (var i = 0; i < raw.Length / bytesPerSample; i++)
            {
                var v = cmyk[s * rowsPerStrip * width * 4 + i];
                if (sixteen) { raw[i * 2] = v; raw[i * 2 + 1] = v; }   // v * 257, little endian
                else raw[i] = v;
            }

            if (predictor)
                for (var r = 0; r < rows; r++)
                {
                    var row = raw.AsSpan(r * rowBytes, rowBytes);
                    for (var i = row.Length - 1; i >= 4; i--) row[i] -= row[i - 4];
                }

            var packed = compression switch
            {
                TiffCompressionMode.Lzw => TiffCodecs.LzwEncode(raw),
                TiffCompressionMode.Deflate => TiffCodecs.DeflateEncode(raw),
                TiffCompressionMode.PackBits => TiffCodecs.PackBitsEncode(raw),
                _ => raw
            };

            offsets[s] = (uint)ms.Position;
            sizes[s] = (uint)packed.Length;
            w.Write(packed);
            if ((packed.Length & 1) == 1) w.Write((byte)0);
        }

        // Out-of-line values
        uint Block(byte[] bytes)
        {
            var at = (uint)ms.Position;
            w.Write(bytes);
            if ((bytes.Length & 1) == 1) w.Write((byte)0);
            return at;
        }

        static byte[] Shorts(params ushort[] v) { var b = new byte[v.Length * 2]; for (var i = 0; i < v.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(i * 2), v[i]); return b; }
        static byte[] Longs(uint[] v) { var b = new byte[v.Length * 4]; for (var i = 0; i < v.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(i * 4), v[i]); return b; }
        static byte[] Rational(double v)
        {
            var b = new byte[8];
            BinaryPrimitives.WriteUInt32LittleEndian(b, (uint)Math.Round(Math.Max(v, 1) * 1000));
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4), 1000);
            return b;
        }

        var sample = (ushort)(sixteen ? 16 : 8);
        var bitsOffset = Block(Shorts(sample, sample, sample, sample));
        var xResOffset = Block(Rational(dpiX > 0 ? dpiX : 96));
        var yResOffset = Block(Rational(dpiY > 0 ? dpiY : 96));
        var stripOffsetsValue = stripCount == 1 ? offsets[0] : Block(Longs(offsets));
        var stripSizesValue = stripCount == 1 ? sizes[0] : Block(Longs(sizes));
        var validIcc = icc is { Length: > 128 } && IsCmykIcc(icc);
        var iccOffset = validIcc ? Block(icc!) : 0u;

        var compressionCode = compression switch
        {
            TiffCompressionMode.Lzw => 5,
            TiffCompressionMode.Deflate => 8,
            TiffCompressionMode.PackBits => 32773,
            _ => 1
        };

        var entries = new List<(ushort tag, ushort type, uint count, uint value)>
        {
            (TagWidth, 4, 1, (uint)width),
            (TagHeight, 4, 1, (uint)height),
            (TagBits, 3, 4, bitsOffset),
            (TagCompression, 3, 1, (uint)compressionCode),
            (TagPhotometric, 3, 1, 5),
            (TagStripOffsets, 4, (uint)stripCount, stripOffsetsValue),
            (TagSamples, 3, 1, 4),
            (TagRowsPerStrip, 4, 1, (uint)rowsPerStrip),
            (TagStripCounts, 4, (uint)stripCount, stripSizesValue),
            (TagXRes, 5, 1, xResOffset),
            (TagYRes, 5, 1, yResOffset),
            (TagPlanar, 3, 1, 1),
            (TagResUnit, 3, 1, 2)
        };
        if (predictor) entries.Add((TagPredictor, 3, 1, 2));
        entries.Add((TagInkSet, 3, 1, 1));
        if (validIcc) entries.Add((TagIcc, 7, (uint)icc!.Length, iccOffset));

        var ifdOffset = (uint)ms.Position;
        w.Write((ushort)entries.Count);
        foreach (var (tag, type, count, value) in entries)
        {
            w.Write(tag); w.Write(type); w.Write(count);
            // Inline SHORT values occupy the first two bytes of the value field.
            if (type == 3 && count == 1) { w.Write((ushort)value); w.Write((ushort)0); }
            else w.Write(value);
        }
        w.Write(0u);

        ms.Position = 4;
        w.Write(ifdOffset);
        w.Flush();

        ms.Position = 0;
        ms.CopyTo(stream);
    }

    /// <summary>
    /// True if an ICC profile header declares the CMYK colour space ('CMYK' at offset 16).
    /// </summary>
    public static bool IsCmykIcc(byte[] icc) =>
        icc.Length > 20 && icc[16] == 'C' && icc[17] == 'M' && icc[18] == 'Y' && icc[19] == 'K';
}