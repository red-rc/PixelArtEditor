using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.Models;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;

namespace PixelArtEditor.ViewModels;

public class IndexedPropertiesVM : ReactiveObject
{
    private List<string> _quantizationNames = [.. Enum.GetNames<PaletteQuantization>()];
    public List<string> QuantizationNames
    {
        get => _quantizationNames;
        private set => this.RaiseAndSetIfChanged(ref _quantizationNames, value);
    }

    private string _quantizationName = "MedianCut";
    public string QuantizationName
    {
        get => _quantizationName;
        set
        {
            if (_quantizationName == value || !QuantizationNames.Contains(value)) return;
            Quantization = StringToEnum<PaletteQuantization>(value);
            this.RaiseAndSetIfChanged(ref _quantizationName, value);
        }
    }

    public PaletteQuantization Quantization = PaletteQuantization.MedianCut;

    public byte ColorCount = 255;

    private int _maxColorCountXaml;
    public int MaxColorCountXaml
    {
        get => _maxColorCountXaml;
        set => this.RaiseAndSetIfChanged(ref _maxColorCountXaml, value);
    }

    private int _colorCountXaml;
    public int ColorCountXaml
    {
        get => _colorCountXaml;
        set
        {
            this.RaiseAndSetIfChanged(ref _colorCountXaml, value);
            ColorCount = (byte)(_colorCountXaml - 1);
        }
    }

    private bool _dither = true;
    public bool Dither
    {
        get => _dither;
        set => this.RaiseAndSetIfChanged(ref _dither, value);
    }

    private static T StringToEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, ignoreCase: false, out var result))
            return result;

        throw new ArgumentException($"{LocalizationService.Get("UnknownValue")} '{value}' " +
            $"{LocalizationService.Get("ForEnum")} {typeof(T).Name}");
    }

    private byte[] _modelData;
    private Palette? _palette;

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public IndexedPropertiesVM(Window dialog, PixelModel model)
    {
        this.WhenAnyValue(x => x.Quantization).Subscribe(_ => UpdatePreview(model));
        this.WhenAnyValue(x => x.ColorCount).Subscribe(_ => UpdatePreview(model));
        this.WhenAnyValue(x => x.Dither).Subscribe(_ => UpdatePreview(model));

        _modelData = model.Data;
        LoadFrom(model);

        ResetCommand = ReactiveCommand.Create(() =>
        {
            LoadFrom(model);
            RenderData.Bitmap = BitmapService.CreateBitmap(model.Data, model.Width, model.Height);
        });

        CancelCommand = ReactiveCommand.Create(() =>
        {
            LoadFrom(model);
            dialog.Close();
        });

        SaveCommand = ReactiveCommand.Create(() =>
        {
            model.Data = _modelData;
            model.Palette = _palette;
            model.NotifyModelChanged();
            dialog.Close();
        });
    }

    public void LoadFrom(PixelModel model)
    {
        RenderData.Width = model.Width;
        RenderData.Height = model.Height;

        MaxColorCountXaml = PaletteService.GetMaxPaletteColorIdx(model.BitDepth) + 1;
        ColorCountXaml = MaxColorCountXaml;

        if (model.Palette is not null)
        {
            Dither = model.Palette.Dither ?? false;
            QuantizationName = model.Palette.QuantizationMethod.ToString() ?? "MedianCut";
        }
    }

    private void UpdatePreview(PixelModel model)
    {
        if (model.Data is null || model.Data.Length == 0) return;
        
        _palette = PaletteService.GetPalette(model.Data, model.Width, ColorCount, Quantization, Dither);
        _modelData = BitmapService.QuantizeToPalette(model.Data, _palette);
        
        if (RenderData.Bitmap is null)
            RenderData.Bitmap = BitmapService.CreateBitmap(_modelData, model.Width, model.Height);
        else
            BitmapService.UpdateBitmap(RenderData.Bitmap, _modelData, new Rect(0, 0, model.Width, model.Height));

        RenderData.NotifyPropertyChanged();
    }
}
