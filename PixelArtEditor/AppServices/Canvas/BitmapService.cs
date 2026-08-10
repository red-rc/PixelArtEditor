using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                var color = isLight ? new Color(255, 235, 235, 235)
                    : new Color(255, 185, 185, 185);
            
                pixelData[i + 0] = color.B;
                pixelData[i + 1] = color.G;
                pixelData[i + 2] = color.R;
                pixelData[i + 3] = color.A;
            }
        }
        
        return pixelData;
    }

    public static void SetPixelData(WriteableBitmap wb, byte[] pixelData)
    {
        unsafe
        {
            using var fb = wb.Lock();
            if (wb.Format is null) throw new InvalidOperationException("Bitmap format is null.");

            var bytesPerPixel = wb.Format.Value.BitsPerPixel / 8;
            var rowBytes = fb.RowBytes;
            fixed (byte* srcPtr = pixelData)
            {
                for (var y = 0; y < wb.PixelSize.Height; y++)
                {
                    byte* dst = (byte*)fb.Address + y * rowBytes;
                    byte* src = srcPtr + y * wb.PixelSize.Width * bytesPerPixel;
                    Buffer.MemoryCopy(src, dst, rowBytes, wb.PixelSize.Width * bytesPerPixel);
                }
            }
        }
    }

    public static WriteableBitmap CreateBitmap(int width, int height, byte[] pixelData)
    {
        var wb = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        SetPixelData(wb, pixelData);

        return wb;
    }

    public static Color GetPixelColor(byte[] pixelData, int width, PixelPoint pixel)
    {
        var stride = width * 4;
        var index = pixel.Y * stride + pixel.X * 4;

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

        foreach (var layer in layers)
        {
            if (!layer.IsVisible) continue;

            var stride = layer.Width * 4;
            var index = pixel.Y * stride + pixel.X * 4;

            var src = layer.PixelData;
            if ((uint)(index + 3) >= (uint)src.Length) continue;

            var srcA = src[index + 3] / 255f * layer.Opacity;
            if (srcA <= 0f) continue;

            var newA = srcA + outA * (1f - srcA);
            if (newA <= 0f) continue;

            r = (byte)((src[index + 2] * srcA + r * outA * (1f - srcA)) / newA);
            g = (byte)((src[index + 1] * srcA + g * outA * (1f - srcA)) / newA);
            b = (byte)((src[index + 0] * srcA + b * outA * (1f - srcA)) / newA);
            outA = newA;

            if (outA >= 0.999f) break;
        }

        return Color.FromArgb((byte)(outA * 255f), r, g, b);
    }

    public static unsafe void FillSimilarPixels(WriteableBitmap? wb, byte[] pixelData, int width, int height, PixelPoint startPixel, Color newColor)
    {
        if (wb == null) return;
    
        var targetColor = GetPixelColor(pixelData, width, startPixel);
        if (targetColor == newColor) return;
    
        var visited = new byte[width * height];
        
        var stack = new Stack<int>(512);
        
        int startIndex = startPixel.Y * width + startPixel.X;
        stack.Push(startIndex);
        visited[startIndex] = 1;
    
        var stride = width * 4;
        
        uint targetPacked = 
            ((uint)targetColor.B) |
            ((uint)targetColor.G << 8) |
            ((uint)targetColor.R << 16) |
            ((uint)targetColor.A << 24);
        
        uint newPacked = 
            ((uint)newColor.B) |
            ((uint)newColor.G << 8) |
            ((uint)newColor.R << 16) |
            ((uint)newColor.A << 24);
    
        fixed (byte* pBase = pixelData)
        {
            uint* pPixels = (uint*)pBase;
            
            while (stack.Count > 0)
            {
                var flat = stack.Pop();
                pPixels[flat] = newPacked;
    
                var x = flat % width;
                var y = flat / width;
    
                if (x + 1 < width)  TryPush(stack, visited, pPixels, flat + 1,         targetPacked);
                if (x - 1 >= 0)     TryPush(stack, visited, pPixels, flat - 1,         targetPacked);
                if (y + 1 < height) TryPush(stack, visited, pPixels, flat + width,     targetPacked);
                if (y - 1 >= 0)     TryPush(stack, visited, pPixels, flat - width,     targetPacked);
            }
        }
    
        SetPixelData(wb, pixelData);
    }
    
    private static unsafe void TryPush(Stack<int> stack, byte[] visited, uint* pPixels, int idx, uint targetPacked)
    {
        if (visited[idx] != 0) return;
        if (pPixels[idx] != targetPacked) return;
        visited[idx] = 1;
        stack.Push(idx);
    }

    public static byte[] ResizePixelData(byte[] src, int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
        var copyWidth = Math.Min(oldWidth, newWidth);
        var copyHeight = Math.Min(oldHeight, newHeight);
        var newData = new byte[newWidth * newHeight * 4];

        for (var y = 0; y < copyHeight; y++)
        {
            var srcOffset = y * oldWidth * 4;
            var dstOffset = y * newWidth * 4;
            Buffer.BlockCopy(src, srcOffset, newData, dstOffset, copyWidth * 4);
        }

        return newData;
    }

    public static byte[] ResizePixelDataScaled(byte[] src, int srcW, int srcH, int dstW, int dstH)
    {
        var dst = new byte[dstW * dstH * 4];

        for (var y = 0; y < dstH; y++)
        {
            var srcY = Math.Min(srcH - 1, (int)((long)y * srcH / dstH));
            for (var x = 0; x < dstW; x++)
            {
                var srcX = Math.Min(srcW - 1, (int)((long)x * srcW / dstW));
                var srcIdx = (srcY * srcW + srcX) * 4;
                var dstIdx = (y * dstW + x) * 4;
                dst[dstIdx + 0] = src[srcIdx + 0];
                dst[dstIdx + 1] = src[srcIdx + 1];
                dst[dstIdx + 2] = src[srcIdx + 2];
                dst[dstIdx + 3] = src[srcIdx + 3];
            }
        }

        return dst;
    }

    public static byte[] CenterOnCanvas(byte[] src, int srcW, int srcH, int canvasW, int canvasH)
    {
        var dst = new byte[canvasW * canvasH * 4];

        var offsetX = (canvasW - srcW) / 2;
        var offsetY = (canvasH - srcH) / 2;

        for (var y = 0; y < srcH; y++)
        {
            var dy = y + offsetY;
            if (dy < 0 || dy >= canvasH) continue;

            for (var x = 0; x < srcW; x++)
            {
                var dx = x + offsetX;
                if (dx < 0 || dx >= canvasW) continue;

                var si = (y * srcW + x) * 4;
                if (si + 3 >= src.Length) continue;

                var di = (dy * canvasW + dx) * 4;

                dst[di + 0] = src[si + 0];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
                dst[di + 3] = src[si + 3];
            }
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
            var srcStride = layer.Width * 4;

            for (var y = 0; y < height; y++)
            {
                if (y >= layer.Height) continue;

                var dstRow = y * width * 4;
                var srcRow = y * srcStride;

                for (var x = 0; x < width; x++)
                {
                    if (x >= layer.Width) continue;

                    var di = dstRow + x * 4;
                    var si = srcRow + x * 4;

                    var srcA = src[si + 3] / 255f * layer.Opacity;
                    var dstA = result[di + 3] / 255f;

                    var outA = srcA + dstA * (1f - srcA);
                    if (outA <= 0f) continue;

                    result[di + 0] = (byte)((src[si + 0] * srcA + result[di + 0] * dstA * (1f - srcA)) / outA);
                    result[di + 1] = (byte)((src[si + 1] * srcA + result[di + 1] * dstA * (1f - srcA)) / outA);
                    result[di + 2] = (byte)((src[si + 2] * srcA + result[di + 2] * dstA * (1f - srcA)) / outA);
                    result[di + 3] = (byte)(outA * 255f);
                }
            }
        }

        return result;
    }

    public static byte[] SwapRB(byte[] rgba)
    {
        var bgra = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            bgra[i + 0] = rgba[i + 2]; // B ← R
            bgra[i + 1] = rgba[i + 1]; // G
            bgra[i + 2] = rgba[i + 0]; // R ← B
            bgra[i + 3] = rgba[i + 3]; // A
        }

        return bgra;
    }
}