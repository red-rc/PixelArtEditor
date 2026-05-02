using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices;

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
        var queue = new Queue<PixelPoint>();
        queue.Enqueue(startPixel);
        visited[startPixel.Y * width + startPixel.X] = 1;

        var stride = width * 4;

        fixed (byte* pBase = pixelData)
        {
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var index = p.Y * stride + p.X * 4;

                pBase[index + 0] = newColor.B;
                pBase[index + 1] = newColor.G;
                pBase[index + 2] = newColor.R;
                pBase[index + 3] = newColor.A;

                var neighbors = new[]
                {
                new PixelPoint(p.X + 1, p.Y),
                new PixelPoint(p.X - 1, p.Y),
                new PixelPoint(p.X, p.Y + 1),
                new PixelPoint(p.X, p.Y - 1)
            };

                foreach (var n in neighbors)
                {
                    if (n.X < 0 || n.X >= width || n.Y < 0 || n.Y >= height) continue;

                    var nIndex = n.Y * width + n.X;
                    if (visited[nIndex] != 0) continue;

                    var neighborIndex = n.Y * stride + n.X * 4;
                    byte b = pBase[neighborIndex + 0];
                    byte g = pBase[neighborIndex + 1];
                    byte r = pBase[neighborIndex + 2];
                    byte a = pBase[neighborIndex + 3];

                    if (b != targetColor.B || g != targetColor.G || r != targetColor.R || a != targetColor.A) continue;

                    queue.Enqueue(n);
                    visited[nIndex] = 1;
                }
            }
        }

        SetPixelData(wb, pixelData);
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
}