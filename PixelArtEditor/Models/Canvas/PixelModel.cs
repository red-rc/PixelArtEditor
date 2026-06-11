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
    public int Width;
    public int Height;

    public float DpiX = 96f;
    public float DpiY = 96f;

    public ColorMode Mode;
    public BitDepth BitDepth;
    public ColorSpace ColorSpace;

    /// <summary>
    /// Чи є окремий альфа-канал у даних.
    /// Для Mode=RGB: false=Rgb24, true=Rgba32.
    /// Для Mode=Grayscale + Bit8: false=L8 (1 байт/піксель), true=La16 (2 байти/піксель).
    /// Для Mode=Grayscale + Bit16: false=L16 (2 байти/піксель), true=La32 (4 байти/піксель).
    /// </summary>
    public bool HasAlphaChannel => Alpha != AlphaFormat.None;

    public AlphaFormat Alpha; // None / Straight / Premultiplied

    /// <summary>
    /// Тільки для Bit16 і RGB565.
    /// </summary>
    public bool BigEndian;

    public byte[] Data = [];
    public Palette? Palette;
}

// RGBA Bit8:    [R,G,B,A, R,G,B,A, ...]
// RGBA Bit16:   [Rlo,Rhi,Glo,Ghi,Blo,Bhi,Alo,Ahi, ...] (little-endian) or [Rhi,Rlo,...] (big-endian)
// Grayscale:    [G, G, G, ...]
// Indexed Bit4: two pixels per byte, high nibble first: [p0p1, p2p3, ...]
// Indexed Bit1: eight pixels per byte, MSB first: [p0p1p2p3p4p5p6p7, ...]
// RGB565:       [lo,hi, lo,hi, ...] packed as RRRRRGGGGGGBBBBB per 2 bytes (little-endian) or reversed (big-endian)