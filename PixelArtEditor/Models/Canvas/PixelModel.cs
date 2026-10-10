using FellowOakDicom;
using ReactiveUI.SourceGenerators;

namespace PixelArtEditor.Models.Canvas;

public enum ColorMode { RGB, RGBA, BGR, BGRA, ARGB, ABGR, RG, A, Grayscale, GrayscaleAlpha, Indexed, CMYK }
public enum BitDepth { Bit1, Bit2, Bit4, Bit8, Bit16, Bit565, Bit5551, Bit1010102 }
public enum AlphaFormat { None, Straight, Premultiplied }

public enum ColorFamily { None, Rgb, Gray, Cmyk }
public enum TransferFunction { SRgb, Linear, Gamma22, Gamma18 }

public sealed record ColorSpace(string Id, ColorFamily Family, TransferFunction Transfer, double[]? RgbToXyz = null)
{
    public static readonly ColorSpace None = new("None", ColorFamily.None, TransferFunction.Linear);

    public static readonly ColorSpace SRgb = new("sRGB", ColorFamily.Rgb, TransferFunction.SRgb,
        [0.4124564, 0.3575761, 0.1804375, 0.2126729, 0.7151522, 0.0721750, 0.0193339, 0.1191920, 0.9503041]);
    public static readonly ColorSpace LinearSRgb = SRgb with { Id = "LinearSRgb", Transfer = TransferFunction.Linear };
    public static readonly ColorSpace DisplayP3 = new("DisplayP3", ColorFamily.Rgb, TransferFunction.SRgb,
        [0.4865709, 0.2656677, 0.1982173, 0.2289746, 0.6917385, 0.0792869, 0.0000000, 0.0451134, 1.0439444]);
    public static readonly ColorSpace AdobeRgb = new("AdobeRGB", ColorFamily.Rgb, TransferFunction.Gamma22,
        [0.5767309, 0.1855540, 0.1881852, 0.2973769, 0.6273491, 0.0752741, 0.0270343, 0.0706872, 0.9911085]);

    public static readonly ColorSpace GraySRgb = new("GraySRGB", ColorFamily.Gray, TransferFunction.SRgb);
    public static readonly ColorSpace GrayG22 = new("GrayG22", ColorFamily.Gray, TransferFunction.Gamma22);
    public static readonly ColorSpace GrayLinear = new("GrayLinear", ColorFamily.Gray, TransferFunction.Linear);

    public static readonly ColorSpace Cmyk = new("CmykIcc", ColorFamily.Cmyk, TransferFunction.Linear);

    public static readonly ColorSpace Custom = new("Custom", ColorFamily.Rgb, TransferFunction.SRgb);

    public static readonly ColorSpace[] All = [SRgb, LinearSRgb, DisplayP3, AdobeRgb, GraySRgb, GrayG22, GrayLinear, Cmyk, Custom, None];
}

public partial class PixelModel : ReactiveObject
{
    [Reactive] public partial string? Name { get; set; }

    private string _extension = "png";
    public string Extension
    {
        get => _extension;
        set => this.RaiseAndSetIfChanged(ref _extension, value);
    }

    [Reactive] public partial int Width { get; set; }
    [Reactive] public partial int Height { get; set; }

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

    [Reactive] public partial ColorMode ColorMode { get; set; }
    [Reactive] public partial BitDepth BitDepth { get; set; }

    private ColorSpace _colorSpace = ColorSpace.SRgb;
    public ColorSpace ColorSpace
    {
        get => _colorSpace;
        set => this.RaiseAndSetIfChanged(ref _colorSpace, value);
    }

    public ColorSpace SrcColorSpace = ColorSpace.SRgb;

    [Reactive] public partial byte[]? IccProfile { get; set; }
    [Reactive] public partial AlphaFormat AlphaFormat { get; set; }
    [Reactive] public partial Palette? Palette { get; set; }

    private byte[] _data = [];
    public byte[] Data
    {
        get => _data;
        set => this.RaiseAndSetIfChanged(ref _data, value);
    }

    [Reactive] public partial DicomDataset? DicomDataset { get; set; }
}

// Contains BGRA data, not RGBA