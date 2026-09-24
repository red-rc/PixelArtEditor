using System;

namespace PixelArtEditor.Models.Canvas;

public enum ColorMode
{
    RGB,
    RGBA,
    Grayscale,
    Indexed
}

public enum BitDepth : byte
{
    Bit1 = 1,
    Bit2 = 2,
    Bit4 = 4,
    Bit8 = 8,
    Bit16 = 16,
    RGB565 = 55 // packed format: R5G6B5, не per-channel
}

public enum AlphaFormat
{
    None,
    Straight,
    Premultiplied
}

public enum ColorSpace
{
    sRGB,
    Linear,
}

public class PixelModel
{
    public string? Name;
    public string Extension = "png";

    public int Width;
    public int Height;

    public float DpiX = 96f;
    public float DpiY = 96f;

    public ColorMode Mode;
    public BitDepth BitDepth;
    public ColorSpace ColorSpace;
    public AlphaFormat Alpha;
    public Palette? Palette;

    public byte[] Data = [];

    public FellowOakDicom.DicomDataset? DicomDataset;

    public event Action? ModelChanged;
    public void NotifyModelChanged() => ModelChanged?.Invoke();
}

// Contains BGRA data, not RGBA
