using Avalonia.Media.Imaging;
using FellowOakDicom;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesUCVM : ReactiveObject
{
    private float _imageProportion = 0f;
    private bool _isUpdating = false;

    private int _width = 32;
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

    private int _height = 32;
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

    // --- DPI (вже були, просто float замість Vector) ---
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

    // --- ColorMode ---
    public List<string> ColorModeNames { get; } = [.. Enum.GetValues<ColorMode>().Select(cm => cm.ToString())];

    private string _colorModeName = "RGBA";
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

    public ColorMode ColorMode = ColorMode.RGBA;

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
    };

    private List<string> _bitDepthNames = [.. ValidBitDepths[ColorMode.RGBA].Select(b => b.ToString())];
    public List<string> BitDepthNames
    {
        get => _bitDepthNames;
        private set => this.RaiseAndSetIfChanged(ref _bitDepthNames, value);
    }

    private string _bitDepthName = "Bit8";
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

    private bool _bitDepthEnabled = true;
    public bool BitDepthEnabled
    {
        get => _bitDepthEnabled;
        set => this.RaiseAndSetIfChanged(ref _bitDepthEnabled, value);
    }

    public BitDepth BitDepth = BitDepth.Bit8;

    // --- ColorSpace ---
    public List<string> ColorSpaceNames { get; } = [..Enum.GetValues<ColorSpace>().Select(cm => cm.ToString())];

    private string _colorSpaceName = "sRGB";
    public string ColorSpaceName
    {
        get => _colorSpaceName;
        set
        {
            if (_colorSpaceName == value) return;
            ColorSpace = EnumHelper.StringToEnum<ColorSpace>(value);
            this.RaiseAndSetIfChanged(ref _colorSpaceName, value);
        }
    }

    public ColorSpace ColorSpace = ColorSpace.sRGB;

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
        [ColorMode.Indexed] = [AlphaFormat.Straight, AlphaFormat.Premultiplied]
    };

    private List<string> _alphaFormatNames = [.. ValidAlphaFormats[ColorMode.RGBA].Select(a => a.ToString())];
    public List<string> AlphaFormatNames
    {
        get => _alphaFormatNames;
        private set => this.RaiseAndSetIfChanged(ref _alphaFormatNames, value);
    }

    private string _alphaFormatName = "Straight";
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

    private bool _alphaFormatEnabled = true;
    public bool AlphaFormatEnabled
    {
        get => _alphaFormatEnabled;
        set => this.RaiseAndSetIfChanged(ref _alphaFormatEnabled, value);
    }

    public AlphaFormat AlphaFormat = AlphaFormat.Straight;

    private void UpdateAvailableProperties()
    {
        var validBitDepths = ValidBitDepths[ColorMode];
        BitDepthNames = [.. validBitDepths.Select(b => b.ToString())];

        if (!validBitDepths.Contains(BitDepth))
            BitDepthName = validBitDepths[0].ToString();

        BitDepthEnabled = validBitDepths.Length > 1;

        var validAlphaFormats = ValidAlphaFormats[ColorMode];
        AlphaFormatNames = [.. validAlphaFormats.Select(a => a.ToString())];

        if (!validAlphaFormats.Contains(AlphaFormat))
            AlphaFormatName = validAlphaFormats[0].ToString();

        AlphaFormatEnabled = validAlphaFormats.Length > 1;
    }

    public PixelModel Model { get; set; } = null!;
    public bool isPaletteSetByUser = false;

    public ReactiveCommand<RxVoid, RxVoid> ConfigureCommand
        => ReactiveCommand.CreateFromTask(async () => await ActionService.ShowIndexedPropertiesWindow(Model));

    private WriteableBitmap? _renderBitmap;
    public WriteableBitmap? RenderBitmap
    {
        get => _renderBitmap;
        set => this.RaiseAndSetIfChanged(ref _renderBitmap, value);
    }

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
            AlphaFormat = AlphaFormat,
            DpiX = DpiX,
            DpiY = DpiY,
            Data = data,
            DicomDataset = dicomDataset
        };
    }
}