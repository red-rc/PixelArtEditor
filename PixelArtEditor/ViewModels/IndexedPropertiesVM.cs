using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models;
using PixelArtEditor.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            Quantization = EnumHelper.StringToEnum<PaletteQuantization>(value);
            this.RaiseAndSetIfChanged(ref _quantizationName, value);
        }
    }

    public PaletteQuantization Quantization = PaletteQuantization.MedianCut;

    private byte _colorIdx = 255;
    public byte ColorIdx
    {
        get => _colorIdx;
        set => this.RaiseAndSetIfChanged(ref _colorIdx, value);
    }

    private byte _maxColorIdx;
    public byte MaxColorIdx
    {
        get => _maxColorIdx;
        set => this.RaiseAndSetIfChanged(ref _maxColorIdx, value);
    }

    private bool _dither = true;
    public bool Dither
    {
        get => _dither;
        set => this.RaiseAndSetIfChanged(ref _dither, value);
    }

    private byte[] _modelData;
    private Palette? _palette;

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    private CancellationTokenSource? _updateCts;

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public IndexedPropertiesVM(Window dialog, PixelModel model)
    {
        _modelData = model.Data;
        LoadFrom(model);

        this.WhenAnyValue(x => x.Quantization, x => x.ColorIdx, x => x.Dither).Subscribe(_ => UpdatePreview(model));

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
        RenderData.Bitmap = BitmapService.CreateBitmap(model.Data, model.Width, model.Height);

        MaxColorIdx = PaletteService.GetMaxPaletteColorIdx(model.BitDepth);
        ColorIdx = MaxColorIdx;

        if (model.Palette is not null)
        {
            Dither = model.Palette.Dither ?? false;
            QuantizationName = model.Palette.QuantizationMethod.ToString() ?? "MedianCut";
        }
    }

    private void UpdatePreview(PixelModel model)
    {
        if (model.Data is null || model.Data.Length == 0) return;

        _updateCts?.Cancel();
        var cts = new CancellationTokenSource();
        _updateCts = cts;
        var token = cts.Token;

        var colorCount = ColorIdx;
        var quantization = Quantization;
        var dither = Dither;
        var data = model.Data;
        var width = model.Width;
        var height = model.Height;

        Task.Run(() =>
        {
            if (token.IsCancellationRequested) return;

            var palette = PaletteService.GetPalette(data, colorCount, quantization);
            if (token.IsCancellationRequested) return;

            var quantized = BitmapService.QuantizeToPalette(data, palette, dither);
            if (token.IsCancellationRequested) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested) return;

                _palette = palette;
                _modelData = quantized;

                if (RenderData.Bitmap is not null)
                    BitmapService.UpdateBitmap(RenderData.Bitmap, _modelData, new Rect(0, 0, width, height));

                RenderData.NotifyPropertyChanged();
            });
        }, CancellationToken.None);
    }
}
