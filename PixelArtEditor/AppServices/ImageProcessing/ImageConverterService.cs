using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.AppServices.ImageProcessing;

public class ImageConverterService
{
    public static byte[] ToIndexed(byte[] bgraData, int width, Palette palette)
    {
        throw new NotImplementedException();
    }

    public static byte[] PackIndices(byte[] indices, BitDepth bitDepth)
    {
        return bitDepth switch
        {
            BitDepth.Bit1 => PackBit1(indices),
            BitDepth.Bit2 => PackBit2(indices),
            BitDepth.Bit4 => PackBit4(indices),
            BitDepth.Bit8 => indices,
            _ => indices
        };
    }

    private static byte[] PackBit1(byte[] indices)
    {
        var packed = new byte[(indices.Length + 7) / 8];
        for (var i = 0; i < indices.Length; i++)
        {
            var byteIdx = i / 8;
            var shift = 7 - (i % 8);
            packed[byteIdx] |= (byte)((indices[i] & 0x01) << shift);
        }

        return packed;
    }

    private static byte[] PackBit2(byte[] indices)
    {
        var packed = new byte[(indices.Length + 3) / 4];
        for (var i = 0; i < indices.Length; i++)
        {
            var byteIdx = i / 4;
            var shift = 6 - (i % 4) * 2;
            packed[byteIdx] |= (byte)((indices[i] & 0x03) << shift);
        }

        return packed;
    }

    private static byte[] PackBit4(byte[] indices)
    {
        var packed = new byte[(indices.Length + 1) / 2];
        for (var i = 0; i < indices.Length; i++)
        {
            var byteIdx = i / 2;
            if (i % 2 == 0)
                packed[byteIdx] = (byte)((indices[i] & 0x0F) << 4);
            else
                packed[byteIdx] |= (byte)(indices[i] & 0x0F);
        }

        return packed;
    }

    public static unsafe byte[] ConvertToGrayscale(byte[] bgraData)
    {
        var result = new byte[bgraData.Length];

        fixed (byte* srcPtr = bgraData)
        fixed (byte* dstPtr = result)
        {
            uint* src = (uint*)srcPtr;
            uint* dst = (uint*)dstPtr;

            for (var i = 0; i < bgraData.Length / 4; i++)
            {
                var packed = src[i];
                var b = packed & 0xFF;
                var g = (packed >> 8) & 0xFF;
                var r = (packed >> 16) & 0xFF;
                var a = (packed >> 24) & 0xFF;
                var gray = (uint)((r * 77 + g * 150 + b * 29) >> 8);
                dst[i] = gray | (gray << 8) | (gray << 16) | (a << 24);
            }
        }

        return result;
    }

    public static unsafe byte[] StripAlpha(byte[] bgraData)
    {
        var result = new byte[bgraData.Length];

        fixed (byte* srcPtr = bgraData)
        fixed (byte* dstPtr = result)
        {
            uint* src = (uint*)srcPtr;
            uint* dst = (uint*)dstPtr;

            for (var i = 0; i < bgraData.Length / 4; i++)
                dst[i] = src[i] | 0xFF000000;
        }

        return result;
    }
}