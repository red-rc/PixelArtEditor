using FellowOakDicom;
using System;

namespace PixelArtEditor.Models.Canvas;

public enum ColorMode
{
    RGB,
    RGBA,
    BGR,
    BGRA,
    ARGB,
    ABGR,
    RG,
    A,
    Grayscale,
    GrayscaleAlpha,
    Indexed
}

public enum BitDepth
{
    Bit1,
    Bit2,
    Bit4,
    Bit8,
    Bit16,
    Bit565,
    Bit5551,
    Bit1010102
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

public class PixelModel : ReactiveObject
{
    private string? _name;
    public string? Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _extension = "png";
    public string Extension
    {
        get => _extension;
        set => this.RaiseAndSetIfChanged(ref _extension, value);
    }

    private int _width;
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private int _height;
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    private float _dpiX = 96f;
    public float DpiX
    {
        get => _dpiX;
        set => this.RaiseAndSetIfChanged(ref _dpiX, value);
    }

    private float _dpiY = 96f;
    public float DpiY
    {
        get => _dpiY;
        set => this.RaiseAndSetIfChanged(ref _dpiY, value);
    }

    private ColorMode _colorMode;
    public ColorMode ColorMode
    {
        get => _colorMode;
        set => this.RaiseAndSetIfChanged(ref _colorMode, value);
    }

    private BitDepth _bitDepth;
    public BitDepth BitDepth
    {
        get => _bitDepth;
        set => this.RaiseAndSetIfChanged(ref _bitDepth, value);
    }

    private ColorSpace _colorSpace;
    public ColorSpace ColorSpace
    {
        get => _colorSpace;
        set => this.RaiseAndSetIfChanged(ref _colorSpace, value);
    }

    private AlphaFormat _alphaFormat;
    public AlphaFormat AlphaFormat
    {
        get => _alphaFormat;
        set => this.RaiseAndSetIfChanged(ref _alphaFormat, value);
    }

    private Palette? _palette;
    public Palette? Palette
    {
        get => _palette;
        set => this.RaiseAndSetIfChanged(ref _palette, value);
    }

    private byte[] _data = [];
    public byte[] Data
    {
        get => _data;
        set => this.RaiseAndSetIfChanged(ref _data, value);
    }

    private DicomDataset? _dicomDataset;
    public DicomDataset? DicomDataset
    {
        get => _dicomDataset;
        set => this.RaiseAndSetIfChanged(ref _dicomDataset, value);
    }
}

// Contains BGRA data, not RGBA