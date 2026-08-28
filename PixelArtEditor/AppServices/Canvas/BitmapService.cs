using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AlphaFormat = Avalonia.Platform.AlphaFormat;

namespace PixelArtEditor.AppServices.Canvas;

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

    public static unsafe void SetPixelData(WriteableBitmap wb, byte[] pixelData, Rect dirtyRect)
    {
        if (wb.Format != PixelFormat.Bgra8888)
            throw new InvalidOperationException("Invalid bitmap format.");

        if (pixelData.Length < wb.PixelSize.Width * wb.PixelSize.Height * 4)
            throw new ArgumentException("pixelData size mismatch with bitmap size.");

        using var fb = wb.Lock();

        var rowBytes = fb.RowBytes;
        var x = (int)dirtyRect.X;
        var startY = (int)dirtyRect.Y;
        var endY = startY + (int)dirtyRect.Height;
        var copyBytes = (int)dirtyRect.Width * 4;

        fixed (byte* srcPtr = pixelData)
        {
            for (var y = startY; y < endY; y++)
            {
                byte* src = srcPtr + (y * wb.PixelSize.Width + x) * 4;
                byte* dst = (byte*)fb.Address + y * rowBytes + x * 4;

                Buffer.MemoryCopy(src, dst, copyBytes, copyBytes);
            }
        }
    }

    public static WriteableBitmap CreateBitmap(int width, int height, byte[] pixelData)
    {
        var wb = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        SetPixelData(wb, pixelData, new Rect(0, 0, width, height));

        return wb;
    }

    public static WriteableBitmap CreateBitmap(int width, int height, Color color)
    {
        var wb = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        var pixelData = Enumerable.Repeat(color, width * height).SelectMany(c => new[] { c.B, c.G, c.R, c.A }).ToArray();

        SetPixelData(wb, pixelData, new Rect(0, 0, width, height));

        return wb;
    }

    public static unsafe void BrushSquare(byte[] pixelData, int width, Rect rect, Color dstColor)
    {
        fixed (byte* ptr = pixelData)
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

            var src = layer.PixelData;
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

    public static unsafe Rect? FillSimilarPixels(byte[] pixelData, int width, PixelPoint startPixel, Color dstColor)
    {
        var srcColor = GetPixelColor(pixelData, width, startPixel);
        if (srcColor == dstColor) return null;

        var height = pixelData.Length / width / 4;
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

        int minX = startPixel.X;
        int maxX = startPixel.X;
        int minY = startPixel.Y;
        int maxY = startPixel.Y;

        fixed (byte* pBase = pixelData)
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

        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static unsafe void TryPush(Stack<int> stack, byte[] visited, uint* pPixels, int idx, uint srcPacked)
    {
        if (visited[idx] != 0 || pPixels[idx] != srcPacked) return;

        visited[idx] = 1;
        stack.Push(idx);
    }

    public static WriteableBitmap GetResizedBitmap(byte[] src, int srcW, int srcH, int dstW, int dstH)
    {
        var resized = ResizePixelData(src, srcW, srcH, dstW, dstH);
        return CreateBitmap(dstW, dstH, resized);
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

    public static byte[] GetCompositePixelData(ObservableCollection<LayerModel> layers, int width, int height)
    {
        var result = new byte[width * height * 4];

        foreach (var layer in layers.Reverse())
        {
            if (!layer.IsVisible) continue;

            var src = layer.PixelData;

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

    public static unsafe byte[] SwapRB(byte[] rgba)
    {
        var bgra = new byte[rgba.Length];

        fixed (byte* srcPtr = rgba)
        fixed (byte* dstPtr = bgra)
        {
            for (var i = 0; i < rgba.Length; i += 4)
            {
                dstPtr[i] = srcPtr[i + 2];      // B ← R
                dstPtr[i + 1] = srcPtr[i + 1];  // G
                dstPtr[i + 2] = srcPtr[i];      // R ← B
                dstPtr[i + 3] = srcPtr[i + 3];  // A
            }
        }

        return bgra;
    }
}