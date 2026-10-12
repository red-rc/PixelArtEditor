using Avalonia.Media.Imaging;
using FellowOakDicom;
using Pixeller.AppServices.Shell;
using Pixeller.Helpers;
using Pixeller.Models;
using Pixeller.Models.Canvas;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixeller.ViewModels;

public partial class ImagePropertiesUCVM(PixelModel model) : ReactiveObject
{
    public PixelModel Model = model;

    private float _imageProportion = 0f;
    private bool _isUpdating = false;

    private int _width = model.Width;
    public int Width
    {
        get => _width;
        set
        {
            if (_isUpdating)
            {
                this.RaiseAndSetIfChanged(ref _width, value);
                return;
            }

            _isUpdating = true;

            if (EnableProportion && _imageProportion != 0f)
            {
                var newHeight = (int)(value / _imageProportion);
                if (newHeight > 0) Height = newHeight;
            }

            this.RaiseAndSetIfChanged(ref _width, value);
            _isUpdating = false;
        }
    }

    private int _height = model.Height;
    public int Height
    {
        get => _height;
        set
        {
            if (_isUpdating)
            {
                this.RaiseAndSetIfChanged(ref _height, value);
                return;
            }

            _isUpdating = true;

            if (EnableProportion && _imageProportion != 0f)
            {
                var newWidth = (int)(value * _imageProportion);
                if (newWidth > 0) Width = newWidth;
            }

            this.RaiseAndSetIfChanged(ref _height, value);
            _isUpdating = false;
        }
    }

    private bool _enableProportion = false;
    public bool EnableProportion
    {
        get => _enableProportion;
        set
        {
            if (Width == 0 || Height == 0) return;
            else if (value) _imageProportion = (float)Width / Height;
            this.RaiseAndSetIfChanged(ref _enableProportion, value);
        }
    }

    // --- DPI ---
    [Reactive] public partial float DpiX { get; set; } = model.DpiX;
    [Reactive] public partial float DpiY { get; set; } = model.DpiY;

    // --- ColorMode ---
    private static string ColorModeToDisplay(string name) => name switch
    {
        "L" => "Grayscale",
        "LA" => "Grayscale + Alpha",
        _ => name
    };

    private static string ColorModeFromDisplay(string name) => name switch
    {
        "Grayscale" => "L",
        "Grayscale + Alpha" => "LA",
        _ => name
    };

    public List<string> ColorModeNames { get; } = [.. Enum.GetValues<ColorMode>().Select(cm => ColorModeToDisplay(cm.ToString()))];

    private string _colorModeName = ColorModeToDisplay(model.ColorMode.ToString());
    public string ColorModeName
    {
        get => _colorModeName;
        set
        {
            var display = ColorModeToDisplay(ColorModeFromDisplay(value));
            if (_colorModeName == display) return;
            ColorMode = EnumHelper.StringToEnum<ColorMode>(ColorModeFromDisplay(value));
            this.RaisePropertyChanged(nameof(IsIndexed));
            this.RaiseAndSetIfChanged(ref _colorModeName, display);

            AlphaFormatEnabled = ColorMode == ColorMode.RGBA || ColorMode == ColorMode.L;
            UpdateAvailableProperties();
        }
    }

    public ColorMode ColorMode = model.ColorMode;

    public bool IsIndexed => ColorMode == ColorMode.Indexed;

    // --- BitDepth ---

    private static readonly Dictionary<ColorMode, BitDepth[]> ValidBitDepths = new()
    {
        [ColorMode.RGBA] = [BitDepth.Bit8, BitDepth.Bit16, BitDepth.Bit1010102],
        [ColorMode.RGB] = [BitDepth.Bit8, BitDepth.Bit16],
        [ColorMode.BGRA] = [BitDepth.Bit4, BitDepth.Bit8, BitDepth.Bit5551],
        [ColorMode.BGR] = [BitDepth.Bit8, BitDepth.Bit565],
        [ColorMode.ARGB] = [BitDepth.Bit8],
        [ColorMode.ABGR] = [BitDepth.Bit8],
        [ColorMode.RG] = [BitDepth.Bit16],
        [ColorMode.A] = [BitDepth.Bit8],
        [ColorMode.L] = [BitDepth.Bit8, BitDepth.Bit16],
        [ColorMode.LA] = [BitDepth.Bit8, BitDepth.Bit16],
        [ColorMode.Indexed] = [BitDepth.Bit1, BitDepth.Bit2, BitDepth.Bit4, BitDepth.Bit8],
        [ColorMode.CMYK] = [BitDepth.Bit8, BitDepth.Bit16]
    };

    [Reactive] public partial List<string> BitDepthNames { get; set; } = 
        [.. ValidBitDepths[model.ColorMode].Select(b => b.ToString())];

    private string _bitDepthName = model.BitDepth.ToString();
    public string BitDepthName
    {
        get => _bitDepthName;
        set 
        {
            if (_bitDepthName == value || !BitDepthNames.Contains(value)) return;
            BitDepth = EnumHelper.StringToEnum<BitDepth>(value);
            this.RaiseAndSetIfChanged(ref _bitDepthName, value);
        }
    }

    [Reactive] public partial bool BitDepthEnabled { get; set; } = true;

    public BitDepth BitDepth = model.BitDepth;

    // --- ColorSpace ---

    private static readonly ColorSpace[] RgbSpaces =
        [ColorSpace.SRgb, ColorSpace.LinearSRgb, ColorSpace.DisplayP3, ColorSpace.AdobeRgb, ColorSpace.Custom];
    private static readonly ColorSpace[] GraySpaces = [ColorSpace.GraySRgb, ColorSpace.GrayG22, ColorSpace.GrayLinear];

    private static readonly Dictionary<ColorMode, ColorSpace[]> ValidColorSpaces = new()
    {
        [ColorMode.RGBA] = RgbSpaces,
        [ColorMode.RGB] = RgbSpaces,
        [ColorMode.BGRA] = RgbSpaces,
        [ColorMode.BGR] = RgbSpaces,
        [ColorMode.ARGB] = RgbSpaces,
        [ColorMode.ABGR] = RgbSpaces,
        [ColorMode.Indexed] = RgbSpaces,
        [ColorMode.L] = GraySpaces,
        [ColorMode.LA] = GraySpaces,
        [ColorMode.RG] = [ColorSpace.None],
        [ColorMode.A] = [ColorSpace.None],
        [ColorMode.CMYK] = [ColorSpace.Cmyk],
    };

    [Reactive] public partial List<string> ColorSpaceNames { get; set; } = 
        [.. ValidColorSpaces[model.ColorMode].Select(c => c.Id)];

    private string _colorSpaceName = model.SrcColorSpace.Id;
    public string ColorSpaceName
    {
        get => _colorSpaceName;
        set
        {
            if (_colorSpaceName == value || !ColorSpaceNames.Contains(value)) return;
            ColorSpace = ColorSpace.All.First(c => c.Id == value);
            this.RaiseAndSetIfChanged(ref _colorSpaceName, value);
        }
    }

    [Reactive] public partial bool ColorSpaceEnabled { get; set; } = true;

    public ColorSpace ColorSpace = model.ColorSpace;

    // --- AlphaFormat ---

    private static readonly Dictionary<ColorMode, AlphaFormat[]> ValidAlphaFormats = new()
    {
        [ColorMode.RGBA] = [AlphaFormat.Straight, AlphaFormat.Premultiplied],
        [ColorMode.RGB] = [AlphaFormat.None, AlphaFormat.Premultiplied],
        [ColorMode.BGRA] = [AlphaFormat.Straight, AlphaFormat.Premultiplied],
        [ColorMode.BGR] = [AlphaFormat.None, AlphaFormat.Premultiplied],
        [ColorMode.ARGB] = [AlphaFormat.Straight, AlphaFormat.Premultiplied],
        [ColorMode.ABGR] = [AlphaFormat.Straight, AlphaFormat.Premultiplied],
        [ColorMode.RG] = [AlphaFormat.None, AlphaFormat.Premultiplied],
        [ColorMode.A] = [AlphaFormat.None],
        [ColorMode.L] = [AlphaFormat.None, AlphaFormat.Premultiplied],
        [ColorMode.LA] = [AlphaFormat.Straight, AlphaFormat.Premultiplied],
        [ColorMode.Indexed] = [AlphaFormat.Straight, AlphaFormat.Premultiplied],
        [ColorMode.CMYK] = [AlphaFormat.None]
    };

    [Reactive] public partial List<string> AlphaFormatNames { get; set; } = 
        [.. ValidAlphaFormats[model.ColorMode].Select(a => a.ToString())];

    private string _alphaFormatName = model.AlphaFormat.ToString();
    public string AlphaFormatName
    {
        get => _alphaFormatName;
        set
        {
            if (_alphaFormatName == value || !AlphaFormatNames.Contains(value)) return;
            AlphaFormat = EnumHelper.StringToEnum<AlphaFormat>(value);
            this.RaiseAndSetIfChanged(ref _alphaFormatName, value);
        }
    }

    [Reactive] public partial bool AlphaFormatEnabled { get; set; } = true;

    public AlphaFormat AlphaFormat = model.AlphaFormat;

    private void UpdateAvailableProperties()
    {
        // BitDepth
        var validBitDepths = ValidBitDepths[ColorMode];
        BitDepthNames = [.. validBitDepths.Select(b => b.ToString())];

        if (!validBitDepths.Contains(BitDepth))
            BitDepthName = validBitDepths.Contains(BitDepth.Bit8) ? BitDepth.Bit8.ToString() : validBitDepths[0].ToString();

        BitDepthEnabled = validBitDepths.Length > 1;

        // ColorSpace
        var validColorSpaces = ValidColorSpaces[ColorMode]
            .Where(c => c != ColorSpace.Custom || Model.IccProfile != null).ToArray();

        ColorSpaceNames = [.. validColorSpaces.Select(c => c.Id)];

        if (!validColorSpaces.Contains(ColorSpace))
            ColorSpaceName = validColorSpaces.Contains(ColorSpace.SRgb) ? ColorSpace.SRgb.Id : validColorSpaces[0].Id;

        ColorSpaceEnabled = validColorSpaces.Length > 1;

        // AlphaFormat
        var validAlphaFormats = ValidAlphaFormats[ColorMode];
        AlphaFormatNames = [.. validAlphaFormats.Select(a => a.ToString())];

        if (!validAlphaFormats.Contains(AlphaFormat))
            AlphaFormatName = validAlphaFormats.Contains(AlphaFormat.Straight) ? AlphaFormat.Straight.ToString() : validAlphaFormats[0].ToString();

        AlphaFormatEnabled = validAlphaFormats.Length > 1;
    }

    // --- Export options (shown only in the export dialog) ---

    public List<string> CompressionNames { get; } = [.. Enum.GetValues<TiffCompressionMode>().Select(c => c.ToString())];

    private string _compressionName = model.Compression.ToString();
    public string CompressionName
    {
        get => _compressionName;
        set
        {
            if (_compressionName == value || !CompressionNames.Contains(value)) return;
            Compression = EnumHelper.StringToEnum<TiffCompressionMode>(value);
            this.RaiseAndSetIfChanged(ref _compressionName, value);
        }
    }

    public TiffCompressionMode Compression = model.Compression;

    [Reactive] public partial int Quality { get; set; } = Math.Clamp(model.Quality, 1, 100);

    public bool isPaletteSetByUser = false;

    public ReactiveCommand<RxVoid, RxVoid> ConfigureCommand
        => ReactiveCommand.CreateFromTask(async () => await ActionService.ShowIndexedPropertiesWindow(Model));

    [Reactive] public partial WriteableBitmap? RenderBitmap { get; set; }

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    public void PushRenderData()
    {
        RenderData.Width = Width;
        RenderData.Height = Height;
        RenderData.Bitmap = RenderBitmap;
    }

    public PixelModel GetFinalPixelModel(byte[] data, string? name, string extension, Palette? palette, DicomDataset? dicomDataset)
    {
        return new PixelModel
        {
            Name = name,
            Extension = extension,
            Palette = palette,
            Width = Width,
            Height = Height,
            ColorMode = ColorMode,
            BitDepth = BitDepth,
            ColorSpace = ColorSpace,
            SrcColorSpace = Model.SrcColorSpace,
            AlphaFormat = AlphaFormat,
            DpiX = DpiX,
            DpiY = DpiY,
            Data = data,
            DicomDataset = dicomDataset,
            IccProfile = Model.IccProfile,
            Compression = Model.Compression
        };
    }
}