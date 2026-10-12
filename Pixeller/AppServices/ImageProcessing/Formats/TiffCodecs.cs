using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Pixeller.AppServices.ImageProcessing.Formats;

/// <summary>
/// Strip codecs used by TIFF: PackBits, TIFF-flavoured LZW (MSB first, early change) and zlib Deflate.
/// </summary>
internal static class TiffCodecs
{
    private const int ClearCode = 256;
    private const int EoiCode = 257;

    // --- PackBits ---

    public static byte[] PackBitsDecode(ReadOnlySpan<byte> src, int expected)
    {
        var res = new byte[expected];
        int i = 0, o = 0;

        while (i < src.Length && o < expected)
        {
            var n = (sbyte)src[i++];
            if (n >= 0)
            {
                var count = Math.Min(n + 1, Math.Min(src.Length - i, expected - o));
                src.Slice(i, count).CopyTo(res.AsSpan(o));
                i += n + 1; o += count;
            }
            else if (n != -128)
            {
                if (i >= src.Length) break;
                var count = Math.Min(1 - n, expected - o);
                res.AsSpan(o, count).Fill(src[i++]);
                o += count;
            }
        }

        return res;
    }

    public static byte[] PackBitsEncode(ReadOnlySpan<byte> src)
    {
        var res = new List<byte>(src.Length + src.Length / 64 + 2);
        var i = 0;

        while (i < src.Length)
        {
            var run = 1;
            while (i + run < src.Length && run < 128 && src[i + run] == src[i]) run++;

            if (run >= 2)
            {
                res.Add((byte)(sbyte)(1 - run));
                res.Add(src[i]);
                i += run;
                continue;
            }

            var start = i;
            var len = 0;
            while (i < src.Length && len < 128)
            {
                if (i + 2 < src.Length && src[i] == src[i + 1] && src[i] == src[i + 2]) break;
                i++; len++;
            }

            res.Add((byte)(len - 1));
            for (var j = start; j < start + len; j++) res.Add(src[j]);
        }

        return [.. res];
    }

    // --- Deflate (zlib) ---

    public static byte[] DeflateDecode(ReadOnlySpan<byte> src, int expected)
    {
        using var input = new MemoryStream(src.ToArray());
        using var z = new ZLibStream(input, CompressionMode.Decompress);
        var res = new byte[expected];
        var read = 0;
        while (read < expected)
        {
            var n = z.Read(res, read, expected - read);
            if (n <= 0) break;
            read += n;
        }
        return res;
    }

    public static byte[] DeflateEncode(ReadOnlySpan<byte> src)
    {
        using var output = new MemoryStream();
        using (var z = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            z.Write(src);
        return output.ToArray();
    }

    // --- LZW ---

    public static byte[] LzwDecode(ReadOnlySpan<byte> src, int expected)
    {
        var res = new byte[expected];
        var prefix = new int[4096];
        var suffix = new byte[4096];
        var first = new byte[4096];
        var length = new int[4096];

        for (var i = 0; i < 256; i++) { suffix[i] = (byte)i; first[i] = (byte)i; length[i] = 1; }

        int bitBuf = 0, bitCount = 0, pos = 0, o = 0;
        int codeSize = 9, next = 258, prev = -1;

        while (o < expected)
        {
            while (bitCount < codeSize)
            {
                if (pos >= src.Length) return res;
                bitBuf = (bitBuf << 8) | src[pos++];
                bitCount += 8;
            }

            var code = (bitBuf >> (bitCount - codeSize)) & ((1 << codeSize) - 1);
            bitCount -= codeSize;
            bitBuf &= (1 << bitCount) - 1;

            if (code == EoiCode) break;

            if (code == ClearCode)
            {
                codeSize = 9; next = 258; prev = -1;
                continue;
            }

            if (prev == -1)
            {
                if (code >= 256) throw new InvalidDataException("Invalid LZW stream.");
                res[o++] = (byte)code;
                prev = code;
                continue;
            }

            int entry;
            if (code < next) entry = code;
            else if (code == next) entry = -1;
            else throw new InvalidDataException("Invalid LZW stream.");

            if (next < 4096)
            {
                prefix[next] = prev;
                suffix[next] = entry == -1 ? first[prev] : first[code];
                first[next] = first[prev];
                length[next] = length[prev] + 1;
            }

            var target = entry == -1 ? next : code;
            if (target >= 4096) throw new InvalidDataException("Invalid LZW stream.");

            var len = length[target];
            var write = Math.Min(len, expected - o);
            var cur = target;
            for (var k = len - 1; k >= 0; k--)
            {
                if (k < write) res[o + k] = suffix[cur];
                cur = prefix[cur];
            }
            o += write;

            if (next < 4096) next++;
            prev = code;

            codeSize = next switch
            {
                >= 2047 => 12,
                >= 1023 => 11,
                >= 511 => 10,
                _ => 9
            };
        }

        return res;
    }

    public static byte[] LzwEncode(ReadOnlySpan<byte> src)
    {
        var output = new List<byte>(src.Length / 2 + 16);
        var dict = new Dictionary<int, int>();
        int bitBuf = 0, bitCount = 0, codeSize = 9, next = 258;

        void Write(int code)
        {
            bitBuf = (bitBuf << codeSize) | code;
            bitCount += codeSize;
            while (bitCount >= 8)
            {
                output.Add((byte)(bitBuf >> (bitCount - 8)));
                bitCount -= 8;
            }
            bitBuf &= (1 << bitCount) - 1;
        }

        void Grow()
        {
            next++;
            codeSize = next switch
            {
                >= 2048 => 12,
                >= 1024 => 11,
                >= 512 => 10,
                _ => 9
            };
        }

        Write(ClearCode);

        if (src.Length > 0)
        {
            int w = src[0];
            for (var i = 1; i < src.Length; i++)
            {
                var c = src[i];
                var key = (w << 8) | c;

                if (dict.TryGetValue(key, out var code))
                {
                    w = code;
                    continue;
                }

                Write(w);
                dict[key] = next;
                Grow();
                w = c;

                if (next >= 4094)
                {
                    Write(ClearCode);
                    dict.Clear();
                    next = 258;
                    codeSize = 9;
                }
            }

            Write(w);
            Grow();
        }

        Write(EoiCode);
        if (bitCount > 0) output.Add((byte)(bitBuf << (8 - bitCount)));

        return [.. output];
    }
}