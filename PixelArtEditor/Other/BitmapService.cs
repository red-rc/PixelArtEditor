using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PixelArtEditor.Other;

public static class BitmapService
{
    public static byte[] CreatePixelData(int width, int height, Color background)
    {
        var stride = width * 4;
        var pixelData = new byte[height * stride];

        for (var i = 0; i < pixelData.Length; i += 4)
        {
            pixelData[i + 0] = background.B;
            pixelData[i + 1] = background.G;
            pixelData[i + 2] = background.R;
            pixelData[i + 3] = background.A;
        }
        return pixelData;
    }
    
    public static byte[] CreateZeroPixelData(int width, int height)
    {
        var stride = width * 4;
        var pixelData = new byte[height * stride];
        
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                var isLight = (x % 2 == 0 && y % 20 == 0) || (x % 2 == 1 && y % 20 == 1);
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
    
    public static WriteableBitmap CreateBitmap(int width, int height, byte[] pixelData)
    {
        var wb = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        using var fb = wb.Lock();
        Marshal.Copy(pixelData, 0, fb.Address, pixelData.Length);

        return wb;
    }
    
    public static void UpdateBitmap(WriteableBitmap bitmap, byte[] pixelData)
    {
        using var fb = bitmap.Lock();
        Marshal.Copy(pixelData, 0, fb.Address, pixelData.Length);
    }
    
    public static Color GetPixelColor(byte[] pixelData, int width, PixelPoint pixel)
    {
        var stride = width * 4;
        var index = pixel.Y * stride + pixel.X * 4;

        if (index < 0 || index + 3 >= pixelData.Length)
            return Colors.Transparent;

        var b = pixelData[index + 0];
        var g = pixelData[index + 1];
        var r = pixelData[index + 2];
        var a = pixelData[index + 3];

        return Color.FromArgb(a, r, g, b);
    }

    public static List<PixelPoint>? GetSimilarPixels(byte[] pixelData, int width, int height, PixelPoint startPixel)
    {
        var result = new List<PixelPoint>();
        var visited = new bool[width * height];

        var targetColor = GetPixelColor(pixelData, width, startPixel);
        
        if (startPixel.X < 0 || startPixel.X >= width || startPixel.Y < 0 || startPixel.Y >= height)
            return null;
        
        var queue = new Queue<PixelPoint>();
        queue.Enqueue(new PixelPoint(startPixel.X, startPixel.Y));
        visited[startPixel.Y * width + startPixel.X] = true;

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            result.Add(p);
            
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
                var index = n.Y * width + n.X;
                if (visited[index]) continue;
                var color = GetPixelColor(pixelData, width, n);
                if (color != targetColor) continue;
                queue.Enqueue(n);
                visited[index] = true;
            }
        }

        return result;
    }
}