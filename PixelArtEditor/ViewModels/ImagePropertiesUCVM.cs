using PixelArtEditor.Other;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesUCVM : ReactiveObject
{
    private float _imageProportion = 0f;
    private bool _isUpdating = false;

    private int _Width = 32;
    public int Width
    {
        get => _Width;
        set
        {
            if (_isUpdating)
            {
                this.RaiseAndSetIfChanged(ref _Width, value);
                return;
            }
            _isUpdating = true;
            if (EnableProportion && _imageProportion != 0f)
            {
                var newHeight = (int)(value / _imageProportion);
                if (newHeight > 0) Height = newHeight;
            }
            this.RaiseAndSetIfChanged(ref _Width, value);
            _isUpdating = false;
        }
    }

    private int _Height = 32;
    public int Height
    {
        get => _Height;
        set
        {
            if (_isUpdating)
            {
                this.RaiseAndSetIfChanged(ref _Height, value);
                return;
            }
            _isUpdating = true;
            if (EnableProportion && _imageProportion != 0f)
            {
                var newWidth = (int)(value * _imageProportion);
                if (newWidth > 0) Width = newWidth;
            }
            this.RaiseAndSetIfChanged(ref _Height, value);
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
    public List<string> ColorModesNames { get; } = [.. Enum.GetValues<ColorMode>().Select(cm => cm.ToString())];

    private string _colorModeName = "RGBA";
    public string ColorModeName
    {
        get => _colorModeName;
        set
        {
            if (_colorModeName == value) return;
            ColorMode = StringToEnum<ColorMode>(value);
            this.RaiseAndSetIfChanged(ref _colorModeName, value);
        }
    }

    public ColorMode ColorMode = ColorMode.RGBA;

    // --- BitDepth ---
    public List<string> BitDepthsNames { get; } = [.. Enum.GetValues<BitDepth>().Select(cm => cm.ToString())];

    private string _bitDepthName = "Bit8";
    public string BitDepthName
    {
        get => _bitDepthName;
        set 
        {
            if (_bitDepthName == value) return;
            BitDepth = StringToEnum<BitDepth>(value);
            this.RaiseAndSetIfChanged(ref _bitDepthName, value);
        }
    }

    public BitDepth BitDepth = BitDepth.Bit8;

    // --- ColorSpace ---
    public List<string> ColorSpacesNames { get; } = [..Enum.GetValues<ColorSpace>().Select(cm => cm.ToString())];

    private string _colorSpaceName = "sRGB";
    public string ColorSpaceName
    {
        get => _colorSpaceName;
        set
        {
            if (_colorSpaceName == value) return;
            ColorSpace = StringToEnum<ColorSpace>(value);
            this.RaiseAndSetIfChanged(ref _colorSpaceName, value);
        }
    }

    public ColorSpace ColorSpace = ColorSpace.sRGB;

    // --- AlphaFormat ---
    public List<string> AlphaFormatNames { get; } = [.. Enum.GetValues<AlphaFormat>().Select(cm => cm.ToString())];

    private string _alphaFormatName = "Straight";
    public string AlphaFormatName
    {
        get => _alphaFormatName;
        set
        {
            if (_alphaFormatName == value) return;
            AlphaFormat = StringToEnum<AlphaFormat>(value);
            this.RaiseAndSetIfChanged(ref _alphaFormatName, value);
        }
    }

    public AlphaFormat AlphaFormat = AlphaFormat.Straight;

    // --- BigEndian ---
    private bool _bigEndian = false;
    public bool BigEndian
    {
        get => _bigEndian;
        set => this.RaiseAndSetIfChanged(ref _bigEndian, value);
    }

    private static T StringToEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, ignoreCase: false, out var result))
            return result;

        throw new ArgumentException($"Unknown value '{value}' for enum {typeof(T).Name}");
    }

    private PixelModel _livePreviewParams = new();
    public PixelModel LivePreviewParams
    {
        get => _livePreviewParams;
        private set => this.RaiseAndSetIfChanged(ref _livePreviewParams, value);
    }

    private bool _isUpdatingPreview = false;

    public ImagePropertiesUCVM()
    {
        this.Changed.Subscribe(_ => UpdateLivePreview());
        UpdateLivePreview();
    }

    private void UpdateLivePreview()
    {
        if (_isUpdatingPreview) return;
        _isUpdatingPreview = true;

        LivePreviewParams = new PixelModel
        {
            Width = Width,
            Height = Height,
            Mode = ColorMode,
            BitDepth = BitDepth,
            ColorSpace = ColorSpace,
            Alpha = AlphaFormat,
            DpiX = DpiX,
            DpiY = DpiY,
            BigEndian = BigEndian,
            Data = []
        };

        _isUpdatingPreview = false;
    }
}