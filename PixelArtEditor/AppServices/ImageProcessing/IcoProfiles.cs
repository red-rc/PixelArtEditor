using Avalonia.Platform;
using PixelArtEditor.Models.Canvas;
using System;
using System.IO;

namespace PixelArtEditor.AppServices.ImageProcessing;

public static class IccProfiles
{
    public static byte[]? Load(ColorSpace colorSpace)
    {
        if (colorSpace == ColorSpace.SRgb || colorSpace.Family is ColorFamily.None or ColorFamily.Cmyk) return null;
        try
        {
            using var stream = AssetLoader.Open(new Uri($"avares://PixelArtEditor/Assets/Icc/{colorSpace.Id}.icc"));
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch { return null; }
    }
}