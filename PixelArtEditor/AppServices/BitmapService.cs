using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices;

public static class BitmapService
{
    public static byte[] CreatePixelData(short width, short height, Color background)
    {
        var pixelData = new byte[height * width * 4];

        for (var i = 0; i < pixelData.Length; i += 4)
        {
            pixelData[i + 0] = background.B;
            pixelData[i + 1] = background.G;
            pixelData[i + 2] = background.R;
            pixelData[i + 3] = background.A;
        }
        return pixelData;
    }
    
    public static byte[] CreateZeroPixelData(short width, short height)
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

    public static WriteableBitmap CreateBitmap(short width, short height, AlphaFormat alphaFormat = AlphaFormat.Unpremul)
    {
        return new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, alphaFormat);
    }

    public static WriteableBitmap CreateBitmap(short width, short height, byte[] pixelData)
    {
        var wb = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        SetPixelData(wb, pixelData);

        return wb;
    }

    public static WriteableBitmap CreateBitmap(short width, short height, byte[] pixelData, Vector dpi, AlphaFormat alphaFormat)
    {
        var wb = new WriteableBitmap(new PixelSize(width, height), dpi, PixelFormat.Bgra8888, alphaFormat);
        SetPixelData(wb, pixelData);

        return wb;
    }

    public static void UpdateBitmapProperties(ref WriteableBitmap wb, short newWidth, short newHeight, Vector dpi, AlphaFormat alphaFormat)
    {
        if (wb == null || wb.Format == null) return;

        var copyWidth = Math.Min(wb.PixelSize.Width, newWidth);
        var copyHeight = Math.Min(wb.PixelSize.Height, newHeight);

        var bpp = wb.Format.Value.BitsPerPixel / 8;

        var newRowBytes = ((newWidth * bpp + 3) / 4) * 4;
        var newPixelData = new byte[newRowBytes * newHeight];

        using var fb = wb.Lock();

        unsafe
        {
            byte* srcBase = (byte*)fb.Address;

            fixed (byte* dstBase = newPixelData)
            {
                for (int y = 0; y < copyHeight; y++)
                {
                    Buffer.MemoryCopy(
                        srcBase + y * fb.RowBytes,
                        dstBase + y * newRowBytes,
                        newRowBytes,
                        copyWidth * bpp
                    );
                }
            }
        }

        wb = CreateBitmap(newWidth, newHeight, newPixelData, dpi, alphaFormat);
    }

    public static byte[] GetPixelData(WriteableBitmap wb)
    {
        if (wb.Format == null) throw new InvalidOperationException("Bitmap format is null.");

        var width = wb.PixelSize.Width;
        var height = wb.PixelSize.Height;
        var bytesPerPixel = wb.Format.Value.BitsPerPixel / 8;

        byte[] pixelData = new byte[width * height * bytesPerPixel];

        unsafe
        {
            using var fb = wb.Lock();

            fixed (byte* dstBase = pixelData)
            {
                for (var y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(
                        (byte*)fb.Address + y * fb.RowBytes,
                        dstBase + y * width * bytesPerPixel,
                        width * bytesPerPixel,
                        width * bytesPerPixel
                    );
                }
            }
        }

        return pixelData;
    }

    public static Color GetPixelColor(byte[] pixelData, short width, PixelPoint pixel)
    {
        var stride = width * 4;
        var index = pixel.Y * stride + pixel.X * 4;

        if (index < 0 || index + 3 >= pixelData.Length) return Colors.Transparent;

        var b = pixelData[index + 0];
        var g = pixelData[index + 1];
        var r = pixelData[index + 2];
        var a = pixelData[index + 3];

        return Color.FromArgb(a, r, g, b);
    }

    public static unsafe void FillSimilarPixels(WriteableBitmap? wb, byte[] pixelData, short width, short height, PixelPoint startPixel, Color newColor)
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
}