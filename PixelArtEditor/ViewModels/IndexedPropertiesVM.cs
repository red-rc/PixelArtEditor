using Avalonia.Controls;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.AppServices.Bitmap.Quantization;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models;
using PixelArtEditor.Models.Canvas;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.ViewModels;

public partial class IndexedPropertiesVM : ReactiveObject
{
    [Reactive] public partial List<string> QuantizationNames { get; set; } = [.. Enum.GetNames<PaletteQuantization>()];

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

    [Reactive] public partial int ColorCount { get; set; }
    [Reactive] public partial int MaxColorCount { get; set; }
    [Reactive] public partial bool Dither { get; set; } = true;

    private Palette? _palette;
    private Dictionary<uint, int>? _histogram;

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    [Reactive] public partial bool IsLoading { get; set; }
    [Reactive] public partial int ProgressValue { get; set; } = 0;

    private CancellationTokenSource? _updateCts;

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public IndexedPropertiesVM(Window dialog, PixelModel model)
    {
        LoadFrom(model);

        this.WhenAnyValue(x => x.QuantizationName, x => x.ColorCount, x => x.Dither).Subscribe(_ => UpdatePreview(model));

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
        RenderData.Bitmap = BitmapService.CreateBitmap(model.Data, model.Width, model.Height);

        _histogram = QuantizationService.GetHistogram(model.Data);

        MaxColorCount = PaletteService.GetPaletteMaxColorCount(model.BitDepth);
        ColorCount = MaxColorCount;

        if (model.Palette is not null)
        {
            Dither = model.Palette.Dither;
            QuantizationName = model.Palette.QuantizationMethod.ToString() ?? "MedianCut";
        }
    }

    private async void UpdatePreview(PixelModel model)
    {
        _updateCts?.Cancel();
        var cts = new CancellationTokenSource();
        _updateCts = cts;
        var token = cts.Token;

        IsLoading = true;
        ProgressValue = 0;

        var progress = new Progress<int>(p =>
        {
            if (token.IsCancellationRequested) return;
            ProgressValue = p;
        });

        var colorCount = ColorCount;
        var quantization = Quantization;
        var dither = Dither;

        try
        {
            var (palette, quantized) = await Task.Run<(Palette?, byte[]?)>(() =>
            {
                if (token.IsCancellationRequested) return (null, null);

                var p = PaletteService.GetPalette(model.Data, colorCount, quantization, dither, ref _histogram);
                if (token.IsCancellationRequested) return (null, null);

                var q = BitmapService.QuantizeToPalette(model.Data, model.Width, p, progress, token);
                return ((Palette?)p, q);
            });

            if (token.IsCancellationRequested || palette is null || quantized is null) return;

            _palette = palette;

            RenderData.Bitmap?.Dispose();
            RenderData.Bitmap = BitmapService.CreateBitmap(quantized, model.Width, model.Height);

            ProgressValue = 100;
        }
        finally
        {
            if (!token.IsCancellationRequested) IsLoading = false;
        }
    }
}
