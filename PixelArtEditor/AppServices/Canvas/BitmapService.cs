using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

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
        var wb = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96));
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
        var bytesPerPixel = 4;
        var copyWidth = Math.Min(oldWidth, newWidth);
        var copyHeight = Math.Min(oldHeight, newHeight);
        var newData = new byte[newWidth * newHeight * bytesPerPixel];

        for (var y = 0; y < copyHeight; y++)
        {
            var srcOffset = y * oldWidth * bytesPerPixel;
            var dstOffset = y * newWidth * bytesPerPixel;
            Buffer.BlockCopy(src, srcOffset, newData, dstOffset, copyWidth * bytesPerPixel);
        }

        return newData;
    }

    public static byte[] RGBAToBGRA(byte[] rgba)
    {
        var bgra = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            bgra[i + 0] = rgba[i + 2]; // B ← R
            bgra[i + 1] = rgba[i + 1]; // G
            bgra[i + 2] = rgba[i + 0]; // R ← B
            bgra[i + 3] = rgba[i + 3]; // A
        }
        bgra.AsSpan().CopyTo(rgba);

        return bgra;
    }
}