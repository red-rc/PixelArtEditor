using Avalonia.Media.Imaging;
using FellowOakDicom;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models;
using PixelArtEditor.Models.Canvas;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.ViewModels;

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
    public List<string> ColorModeNames { get; } = [.. Enum.GetValues<ColorMode>().Select(cm => cm.ToString())];

    private string _colorModeName = model.ColorMode.ToString();
    public string ColorModeName
    {
        get => _colorModeName;
        set
        {
            if (_colorModeName == value) return;
            ColorMode = EnumHelper.StringToEnum<ColorMode>(value);
            this.RaisePropertyChanged(nameof(IsIndexed));
            this.RaiseAndSetIfChanged(ref _colorModeName, value);

            AlphaFormatEnabled = ColorMode == ColorMode.RGBA || ColorMode == ColorMode.Grayscale;
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
        [ColorMode.Grayscale] = [BitDepth.Bit8, BitDepth.Bit16],
        [ColorMode.GrayscaleAlpha] = [BitDepth.Bit8, BitDepth.Bit16],
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
        [ColorMode.Grayscale] = GraySpaces,
        [ColorMode.GrayscaleAlpha] = GraySpaces,
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
        [ColorMode.Grayscale] = [AlphaFormat.None, AlphaFormat.Premultiplied],
        [ColorMode.GrayscaleAlpha] = [AlphaFormat.Straight, AlphaFormat.Premultiplied],
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
            BitDepthName = validBitDepths[0].ToString();

        BitDepthEnabled = validBitDepths.Length > 1;

        // ColorSpace
        var validColorSpaces = ValidColorSpaces[ColorMode]
            .Where(c => c != ColorSpace.Custom || Model.IccProfile != null).ToArray();

        ColorSpaceNames = [.. validColorSpaces.Select(c => c.Id)];

        if (!validColorSpaces.Contains(ColorSpace))
            ColorSpaceName = validColorSpaces[0].Id;

        ColorSpaceEnabled = validColorSpaces.Length > 1;

        // AlphaFormat
        var validAlphaFormats = ValidAlphaFormats[ColorMode];
        AlphaFormatNames = [.. validAlphaFormats.Select(a => a.ToString())];

        if (!validAlphaFormats.Contains(AlphaFormat))
            AlphaFormatName = validAlphaFormats[0].ToString();

        AlphaFormatEnabled = validAlphaFormats.Length > 1;
    }

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
            IccProfile = Model.IccProfile
        };
    }
}