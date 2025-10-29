using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
namespace PixelArtEditor.AppServices;

public interface IBitmapService
{
    byte[] CreatePixelData(int width, int height, Color background);
    byte[] CreateZeroPixelData(int width, int height);
    byte[] CreateScaledPixelData(byte[] source, int srcWidth, int srcHeight, int newWidth, int newHeight);

    WriteableBitmap CreateBitmap(int width, int height, byte[] pixelData);
    void UpdateBitmap(WriteableBitmap bitmap, byte[] pixelData);

    Color GetPixelColor(byte[] pixelData, int width, PixelPoint pixel);
    List<PixelPoint>? GetSimilarPixels(byte[] pixelData, int width, int height, PixelPoint startPixel);
}
