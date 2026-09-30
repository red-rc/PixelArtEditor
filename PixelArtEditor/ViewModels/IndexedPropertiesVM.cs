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

    private int _colorCount;
    public int ColorCount
    {
        get => _colorCount;
        set => this.RaiseAndSetIfChanged(ref _colorCount, value);
    }

    private int _maxColorCount;
    public int MaxColorCount
    {
        get => _maxColorCount;
        set => this.RaiseAndSetIfChanged(ref _maxColorCount, value);
    }

    private bool _dither = true;
    public bool Dither
    {
        get => _dither;
        set => this.RaiseAndSetIfChanged(ref _dither, value);
    }

    private Palette? _palette;

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private CancellationTokenSource? _updateCts;

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public IndexedPropertiesVM(Window dialog, PixelModel model)
    {
        LoadFrom(model);

        this.WhenAnyValue(x => x.Quantization, x => x.ColorCount, x => x.Dither).Subscribe(_ => UpdatePreview(model));

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
            model.Palette = _palette;
            dialog.Close();
        });
    }

    public void LoadFrom(PixelModel model)
    {
        RenderData.Width = model.Width;
        RenderData.Height = model.Height;

        MaxColorCount = PaletteService.GetPaletteMaxColorCount(model.BitDepth);
        ColorCount = MaxColorCount;

        if (model.Palette is not null)
        {
            Dither = model.Palette.Dither ?? false;
            QuantizationName = model.Palette.QuantizationMethod.ToString() ?? "MedianCut";
        }
    }

    private void UpdatePreview(PixelModel model)
    {
        if (RenderData.Bitmap is null) IsLoading = true;

        _updateCts?.Cancel();
        var cts = new CancellationTokenSource();
        _updateCts = cts;
        var token = cts.Token;

        var colorCount = ColorCount;
        var quantization = Quantization;
        var dither = Dither;

        Task.Run(() =>
        {
            if (token.IsCancellationRequested) return;

            var palette = PaletteService.GetPalette(model.Data, colorCount, quantization);
            if (token.IsCancellationRequested) return;

            var quantized = BitmapService.QuantizeToPalette(model.Data, palette, dither);
            if (token.IsCancellationRequested) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested) return;

                _palette = palette;

                RenderData.Bitmap?.Dispose();
                RenderData.Bitmap = BitmapService.CreateBitmap(quantized, model.Width, model.Height);

                IsLoading = false;
            });
        }, CancellationToken.None);
    }
}
