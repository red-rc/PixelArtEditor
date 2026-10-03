using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AlphaFormat = Avalonia.Platform.AlphaFormat;

namespace PixelArtEditor.AppServices.Bitmap;

public static class BitmapService
{
    public static byte[] CreateCheckerBoardPixelData(int width, int height)
    {
        var pixelData = new byte[height * width * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                var isLight = x % 2 == 0 && y % 2 == 0 || x % 2 == 1 && y % 2 == 1;
                var color = isLight ? new Color(255, 235, 235, 235) : new Color(255, 185, 185, 185);

                pixelData[i + 0] = color.B;
                pixelData[i + 1] = color.G;
                pixelData[i + 2] = color.R;
                pixelData[i + 3] = color.A;
            }
        }

        return pixelData;
    }

    public static unsafe WriteableBitmap CreateBitmap(byte[] data, int width, int height)
    {
        var wb = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        if (data.Length < wb.PixelSize.Width * wb.PixelSize.Height * 4)
            throw new ArgumentException(LocalizationService.Get("InvalidPixelData"));

        using var fb = wb.Lock();
        var bytes = width * 4;

        fixed (byte* srcPtr = data)
        {
            for (var y = 0; y < height; y++)
                Buffer.MemoryCopy(srcPtr + (y * wb.PixelSize.Width) * 4, (byte*)fb.Address + y * fb.RowBytes, bytes, bytes);
        }

        return wb;
    }

    public static unsafe void BrushSquare(byte[] data, int width, PixelRect rect, Color dstColor)
    {
        fixed (byte* ptr = data)
        {
            uint color =
                (uint)dstColor.B |
                ((uint)dstColor.G << 8) |
                ((uint)dstColor.R << 16) |
                ((uint)dstColor.A << 24);

            byte* row = ptr + ((int)rect.Y * width * 4) + ((int)rect.X * 4);

            for (var y = 0; y < rect.Height; y++)
            {
                uint* pixel = (uint*)row;

                for (var x = 0; x < rect.Width; x++)
                    pixel[x] = color;

                row += width * 4;
            }
        }
    }

    public static Color GetPixelColor(byte[] pixelData, int width, PixelPoint pixel)
    {
        var index = (pixel.Y * width + pixel.X) * 4;

        if ((uint)(index + 3) >= (uint)pixelData.Length) return Colors.Transparent;

        var b = pixelData[index + 0];
        var g = pixelData[index + 1];
        var r = pixelData[index + 2];
        var a = pixelData[index + 3];

        return Color.FromArgb(a, r, g, b);
    }

    public static Color GetCompositePixelColor(IEnumerable<LayerModel> layers, PixelPoint pixel)
    {
        byte r = 0, g = 0, b = 0;
        float outA = 0f;

        foreach (var layer in layers.Reverse())
        {
            if (!layer.IsVisible) continue;

            var index = (pixel.Y * layer.Width + pixel.X) * 4;

            var src = layer.Data;
            if ((uint)(index + 3) >= (uint)src.Length) continue;

            var srcA = src[index + 3] / 255f * layer.Opacity;
            if (srcA <= 0f) continue;

            var newA = srcA + outA * (1f - srcA);
            if (newA <= 0f) continue;

            b = (byte)((src[index + 0] * srcA + b * outA * (1f - srcA)) / newA);
            g = (byte)((src[index + 1] * srcA + g * outA * (1f - srcA)) / newA);
            r = (byte)((src[index + 2] * srcA + r * outA * (1f - srcA)) / newA);
            outA = newA;

            if (outA >= 0.999f) break;
        }

        return Color.FromArgb(255, r, g, b);
    }

    public static byte[] GetCompositePixelData(ObservableCollection<LayerModel> layers, int width, int height)
    {
        var result = new byte[width * height * 4];

        foreach (var layer in layers.Reverse())
        {
            if (!layer.IsVisible) continue;

            var src = layer.Data;

            for (var y = 0; y < height; y++)
            {
                if (y >= layer.Height) continue;

                var dstRow = y * width * 4;
                var srcRow = y * layer.Width * 4;

                for (var x = 0; x < width; x++)
                {
                    if (x >= layer.Width) continue;

                    var srcIdx = srcRow + x * 4;
                    var dstIdx = dstRow + x * 4;

                    var srcA = src[srcIdx + 3] / 255f * layer.Opacity;
                    var dstA = result[dstIdx + 3] / 255f;

                    var outA = srcA + dstA * (1f - srcA);
                    if (outA <= 0f) continue;

                    result[dstIdx + 0] = (byte)((src[srcIdx + 0] * srcA + result[dstIdx + 0] * dstA * (1f - srcA)) / outA);
                    result[dstIdx + 1] = (byte)((src[srcIdx + 1] * srcA + result[dstIdx + 1] * dstA * (1f - srcA)) / outA);
                    result[dstIdx + 2] = (byte)((src[srcIdx + 2] * srcA + result[dstIdx + 2] * dstA * (1f - srcA)) / outA);
                    result[dstIdx + 3] = (byte)(outA * 255f);
                }
            }
        }

        return result;
    }

    public static unsafe PixelRect? FillSimilarPixels(byte[] data, int width, PixelPoint startPixel, Color dstColor)
    {
        var srcColor = GetPixelColor(data, width, startPixel);
        if (srcColor == dstColor) return null;

        var height = data.Length / width / 4;
        var visited = new byte[width * height];
        var stack = new Stack<int>(512);

        var startIdx = startPixel.Y * width + startPixel.X;
        stack.Push(startIdx);
        visited[startIdx] = 1;

        uint srcPacked =
            ((uint)srcColor.B) |
            ((uint)srcColor.G << 8) |
            ((uint)srcColor.R << 16) |
            ((uint)srcColor.A << 24);

        uint dstPacked =
            ((uint)dstColor.B) |
            ((uint)dstColor.G << 8) |
            ((uint)dstColor.R << 16) |
            ((uint)dstColor.A << 24);

        var minX = startPixel.X;
        var maxX = startPixel.X;
        var minY = startPixel.Y;
        var maxY = startPixel.Y;

        fixed (byte* pBase = data)
        {
            uint* pPixels = (uint*)pBase;

            while (stack.Count > 0)
            {
                var flat = stack.Pop();
                pPixels[flat] = dstPacked;

                var x = flat % width;
                var y = flat / width;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                if (x + 1 < width) TryPush(stack, visited, pPixels, flat + 1, srcPacked);
                if (x - 1 >= 0) TryPush(stack, visited, pPixels, flat - 1, srcPacked);
                if (y + 1 < height) TryPush(stack, visited, pPixels, flat + width, srcPacked);
                if (y - 1 >= 0) TryPush(stack, visited, pPixels, flat - width, srcPacked);
            }
        }

        return new PixelRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static unsafe void TryPush(Stack<int> stack, byte[] visited, uint* pPixels, int idx, uint srcPacked)
    {
        if (visited[idx] != 0) return;

        uint pixel = pPixels[idx];
        bool isSame = (srcPacked >> 24) == 0 ? (pixel >> 24) == 0 : pixel == srcPacked;

        if (!isSame) return;
        visited[idx] = 1;
        stack.Push(idx);
    }

    public static unsafe WriteableBitmap DownscaleBox(byte[] src, int srcW, int srcH, int dstW, int dstH)
    {
        var result = new WriteableBitmap(new PixelSize(dstW, dstH), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        var columnBounds = new int[dstW + 1];
        for (var column = 0; column <= dstW; column++)
            columnBounds[column] = (int)((long)column * srcW / dstW);

        var rowBounds = new int[dstH + 1];
        for (var row = 0; row <= dstH; row++)
            rowBounds[row] = (int)((long)row * srcH / dstH);

        using var dstBuffer = result.Lock();
        var dstAddress = dstBuffer.Address;
        var dstRowBytes = dstBuffer.RowBytes;

        fixed (byte* srcPtr = src)
        {
            var srcBase = srcPtr;

            Parallel.For(0, dstH, dstY =>
            {
                var dstRow = (byte*)dstAddress + (long)dstY * dstRowBytes;
                var srcStartY = rowBounds[dstY];
                var srcEndY = Math.Max(rowBounds[dstY + 1], srcStartY + 1);

                for (var dstX = 0; dstX < dstW; dstX++)
                {
                    var srcStartX = columnBounds[dstX];
                    var srcEndX = Math.Max(columnBounds[dstX + 1], srcStartX + 1);

                    ulong sumB = 0, sumG = 0, sumR = 0, sumA = 0;

                    for (var srcY = srcStartY; srcY < srcEndY; srcY++)
                    {
                        var pixel = srcBase + ((long)srcY * srcW + srcStartX) * 4;

                        for (var srcX = srcStartX; srcX < srcEndX; srcX++, pixel += 4)
                        {
                            ulong a = pixel[3];
                            sumB += pixel[0] * a;
                            sumG += pixel[1] * a;
                            sumR += pixel[2] * a;
                            sumA += a;
                        }
                    }

                    var dstPixel = dstRow + dstX * 4;

                    if (sumA == 0)
                    {
                        *(uint*)dstPixel = 0;
                        continue;
                    }

                    var pxCount = (ulong)((srcEndX - srcStartX) * (srcEndY - srcStartY));

                    dstPixel[0] = (byte)(sumB / sumA);
                    dstPixel[1] = (byte)(sumG / sumA);
                    dstPixel[2] = (byte)(sumR / sumA);
                    dstPixel[3] = (byte)(sumA / pxCount);
                }
            });
        }

        return result;
    }

    public static unsafe void DownscaleBoxRegion(byte[] src, int srcW, int srcH, WriteableBitmap dst, PixelRect dstRegion)
    {
        var dstW = dst.PixelSize.Width;
        var dstH = dst.PixelSize.Height;

        using var buffer = dst.Lock();
        var dstAddress = buffer.Address;
        var dstRowBytes = buffer.RowBytes;

        fixed (byte* srcPtr = src)
        {
            var srcBase = srcPtr;

            for (var dstY = dstRegion.Y; dstY < dstRegion.Bottom; dstY++)
            {
                var srcStartY = (int)((long)dstY * srcH / dstH);
                var srcEndY = Math.Max((int)((long)(dstY + 1) * srcH / dstH), srcStartY + 1);
                var dstRow = (byte*)dstAddress + (long)dstY * dstRowBytes;

                for (var dstX = dstRegion.X; dstX < dstRegion.Right; dstX++)
                {
                    var srcStartX = (int)((long)dstX * srcW / dstW);
                    var srcEndX = Math.Max((int)((long)(dstX + 1) * srcW / dstW), srcStartX + 1);

                    ulong sumB = 0, sumG = 0, sumR = 0, sumA = 0;

                    for (var srcY = srcStartY; srcY < srcEndY; srcY++)
                    {
                        var pixel = srcBase + ((long)srcY * srcW + srcStartX) * 4;
                        for (var srcX = srcStartX; srcX < srcEndX; srcX++, pixel += 4)
                        {
                            ulong a = pixel[3];
                            sumB += pixel[0] * a;
                            sumG += pixel[1] * a;
                            sumR += pixel[2] * a;
                            sumA += a;
                        }
                    }

                    var dstPixel = dstRow + dstX * 4;

                    if (sumA == 0)
                    {
                        *(uint*)dstPixel = 0;
                        continue;
                    }

                    var pxCount = (ulong)((srcEndX - srcStartX) * (srcEndY - srcStartY));
                    dstPixel[0] = (byte)(sumB / sumA);
                    dstPixel[1] = (byte)(sumG / sumA);
                    dstPixel[2] = (byte)(sumR / sumA);
                    dstPixel[3] = (byte)(sumA / pxCount);
                }
            }
        }
    }

    public static PixelRect ToThumbRegion(PixelRect rect, int srcW, int srcH, int thumbW, int thumbH)
    {
        var x0 = Math.Max(0, (int)((long)rect.X * thumbW / srcW) - 1);
        var y0 = Math.Max(0, (int)((long)rect.Y * thumbH / srcH) - 1);
        var x1 = Math.Min(thumbW, (int)(((long)rect.Right * thumbW + srcW - 1) / srcW) + 1);
        var y1 = Math.Min(thumbH, (int)(((long)rect.Bottom * thumbH + srcH - 1) / srcH) + 1);

        return new PixelRect(x0, y0, Math.Max(1, x1 - x0), Math.Max(1, y1 - y0));
    }

    public static byte[] ResizePixelData(byte[] src, int srcW, int srcH, int dstW, int dstH)
    {
        var copyWidth = Math.Min(srcW, dstW);
        var copyHeight = Math.Min(srcH, dstH);
        var newData = new byte[dstW * dstH * 4];

        for (var y = 0; y < copyHeight; y++)
        {
            var srcOffset = y * srcW * 4;
            var dstOffset = y * dstW * 4;

            Buffer.BlockCopy(src, srcOffset, newData, dstOffset, copyWidth * 4);
        }

        return newData;
    }

    public static unsafe byte[] ResizePixelDataScaled(byte[] src, int srcW, int srcH, int dstW, int dstH)
    {
        var dst = new byte[dstW * dstH * 4];

        fixed (byte* srcPtr = src)
        fixed (byte* dstPtr = dst)
        {
            for (var y = 0; y < dstH; y++)
            {
                var srcY = Math.Min(srcH - 1, (int)((uint)y * srcH / dstH));
                var srcRow = srcPtr + (nint)srcY * srcW * 4;
                var dstRow = dstPtr + (nint)y * dstW * 4;

                for (var x = 0; x < dstW; x++)
                {
                    var srcX = Math.Min(srcW - 1, (int)((uint)x * srcW / dstW));
                    var s = srcRow + srcX * 4;
                    var d = dstRow + x * 4;

                    d[0] = s[0];
                    d[1] = s[1];
                    d[2] = s[2];
                    d[3] = s[3];
                }
            }
        }

        return dst;
    }

    public static byte[] CenterOnCanvas(byte[] src, int srcW, int srcH, int canvasW, int canvasH)
    {
        var dst = new byte[canvasW * canvasH * 4];

        var offsetX = (canvasW - srcW) / 2;
        var offsetY = (canvasH - srcH) / 2;

        var xStart = Math.Max(0, -offsetX);
        var xEnd = Math.Min(srcW, canvasW - offsetX);
        if (xEnd <= xStart) return dst;

        var rowBytes = (xEnd - xStart) * 4;
        var srcRowOffset = xStart * 4;
        var dstRowOffset = (xStart + offsetX) * 4;

        for (var y = 0; y < srcH; y++)
        {
            var dy = y + offsetY;
            if (dy < 0 || dy >= canvasH) continue;

            var srcOffset = y * srcW * 4 + srcRowOffset;
            var dstOffset = dy * canvasW * 4 + dstRowOffset;

            Buffer.BlockCopy(src, srcOffset, dst, dstOffset, rowBytes);
        }

        return dst;
    }

    public static (int w, int h) FitToCanvas(int srcW, int srcH, int canvasW, int canvasH)
    {
        if (srcW <= canvasW && srcH <= canvasH) return (srcW, srcH);

        var scale = Math.Min((double)canvasW / srcW, (double)canvasH / srcH);
        return (Math.Max(1, (int)(srcW * scale)), Math.Max(1, (int)(srcH * scale)));
    }

    public static unsafe byte[] SwapRB(byte[] data)
    {
        var bgra = new byte[data.Length];

        fixed (byte* srcPtr = data)
        fixed (byte* dstPtr = bgra)
        {
            for (var i = 0; i < data.Length; i += 4)
            {
                dstPtr[i] = srcPtr[i + 2];      // B ← R
                dstPtr[i + 1] = srcPtr[i + 1];  // G
                dstPtr[i + 2] = srcPtr[i];      // R ← B
                dstPtr[i + 3] = srcPtr[i + 3];  // A
            }
        }

        return bgra;
    }

    public static unsafe byte[] QuantizeToPalette(byte[] data, Palette palette, bool dither)
    {
        if (palette.Colors.Count == 0) return data;

        var hasTransparentInPalette = palette.Colors.Any(c => c.A == 0);
        var result = new byte[data.Length];

        if (!palette.Colors.Any(c => c.A != 0)) return result;

        var cube = PaletteLookup.BuildLookupCube(palette);

        fixed (byte* srcPtr = data)
        fixed (byte* dstPtr = result)
        {
            uint* src = (uint*)srcPtr;
            uint* dst = (uint*)dstPtr;

            for (var i = 0; i < data.Length / 4; i++)
            {
                var a = (byte)((src[i] >> 24) & 0xFF);

                if (a < 128 && hasTransparentInPalette) { dst[i] = 0; continue; }

                var b = (byte)(src[i] & 0xFF);
                var g = (byte)((src[i] >> 8) & 0xFF);
                var r = (byte)((src[i] >> 16) & 0xFF);

                var best = PaletteLookup.Lookup(cube, Color.FromArgb(255, r, g, b));
                dst[i] = (uint)best.B | ((uint)best.G << 8) | ((uint)best.R << 16) | (255u << 24);
            }
        }

        return result;
    }

    public static (byte[], Palette palette) GetQuantized(byte[] data, BitDepth bitDepth, Palette? palette = null)
    {
        var maxColorCount = PaletteService.GetPaletteMaxColorCount(bitDepth);

        byte[] newData;
        if (palette is Palette p && palette.Colors.Count <= maxColorCount)
        {
            newData = QuantizeToPalette(data, palette, palette?.Dither ?? false);
            return (newData, p);
        }

        var newPalette = PaletteService.GetPalette(data, maxColorCount, 
            palette?.QuantizationMethod ?? PaletteQuantization.MedianCut);

        newData = QuantizeToPalette(data, newPalette, palette?.Dither ?? false);
        return (newData, newPalette);
    }
}