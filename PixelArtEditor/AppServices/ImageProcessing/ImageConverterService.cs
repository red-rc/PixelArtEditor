using PixelArtEditor.Models.Canvas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.Collections.Generic;
using Color = Avalonia.Media.Color;

namespace PixelArtEditor.AppServices.ImageProcessing;

public class ImageConverterService
{
    public static (byte[] data, Palette palette) ToIndexed(byte[] bgraData, int width, BitDepth bitDepth, 
        int? colorCount = null, PaletteQuantization? quantization = PaletteQuantization.MedianCut, bool? dither = null)
    {
        //byte maxColors;
        //if (colorCount is not null)
        //{
        //    maxColors = (byte)(colorCount - 1) <= GetMaxPaletteColorIdx(bitDepth)
        //        ? (byte)(colorCount - 1) : GetMaxPaletteColorIdx(bitDepth);
        //}
        //else
        //    maxColors = GetMaxPaletteColorIdx(bitDepth);
        //using var image = Image.LoadPixelData<Bgra32>(bgraData, width, height);
        //var quantizer = new OctreeQuantizer(new QuantizerOptions { MaxColors = maxColors });
        //
        //image.Mutate(ctx => ctx.Quantize(quantizer));
        //
        //var colorMap = new Dictionary<uint, byte>();
        //
        //var indices = new byte[bgraData.Length / 4];
        //var paletteColors = new List<Color>();
        //
        //image.ProcessPixelRows(accessor =>
        //{
        //    for (var y = 0; y < accessor.Height; y++)
        //    {
        //        var row = accessor.GetRowSpan(y);
        //        for (var x = 0; x < accessor.Width; x++)
        //        {
        //            var pixel = row[x];
        //            var packed = ((uint)pixel.A << 24) | ((uint)pixel.R << 16) | ((uint)pixel.G << 8) | pixel.B;
        //
        //            if (!colorMap.TryGetValue(packed, out byte value))
        //            {
        //                value = (byte)paletteColors.Count;
        //
        //                colorMap.Add(packed, value);
        //                paletteColors.Add(Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
        //            }
        //
        //            indices[y * width + x] = value;
        //        }
        //    }
        //});

            //return (PackIndices(indices, bitDepth), new Palette([.. paletteColors]));
        throw new NotImplementedException();
    }

    // Pack according to bit depth
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

    public static Color ResolveColorForPalette(Color color, Palette palette, BitDepth bitDepth)
    {
        var colors = palette.Colors;

        for (var i = 0; i < colors.Count; i++)
        {
            if (colors[i] == color)
                return color;
        }

        if (colors.Count < GetMaxPaletteColorIdx(bitDepth) + 1)
        {
            palette.Colors.Add(color);
            return color;
        }

        return BitmapService.GetClosestPaletteColor(color, palette);
    }

    public static byte GetMaxPaletteColorIdx(BitDepth bitDepth) => bitDepth switch
    {
        BitDepth.Bit1 => 1,
        BitDepth.Bit2 => 3,
        BitDepth.Bit4 => 15,
        BitDepth.Bit8 => 255,
        _ => 255
    };

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