using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System;
using System.Reactive;

namespace PixelArtEditor.ViewModels;

public class CreateDialogVM : ReactiveObject
{
    private Color _backgroundColor = Colors.White;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => this.RaiseAndSetIfChanged(ref _backgroundColor, value);
    }

    public ImagePropertiesUCVM ImageProperties { get; }

    // LivePreviewParams тепер береться з ImageProperties і розширюється кольором
    private PixelModel _livePreviewParams = new();
    public PixelModel LivePreviewParams
    {
        get => _livePreviewParams;
        private set => this.RaiseAndSetIfChanged(ref _livePreviewParams, value);
    }

    private LayerModel _layer = null!;
    public LayerModel Layer
    {
        get => _layer;
        private set => this.RaiseAndSetIfChanged(ref _layer, value);
    }

    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public CreateDialogVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        CreateCommand = ReactiveCommand.Create(() =>
        {
            var data = PixelModelService.CreateRgba32(
                ImageProperties.Width,
                ImageProperties.Height,
                BackgroundColor);

            dialog.Close(new PixelModel
            {
                Width = ImageProperties.Width,
                Height = ImageProperties.Height,
                Mode = ImageProperties.ColorMode,
                BitDepth = ImageProperties.BitDepth,
                ColorSpace = ImageProperties.ColorSpace,
                Alpha = ImageProperties.AlphaFormat,
                BigEndian = ImageProperties.BigEndian,
                DpiX = ImageProperties.DpiX,
                DpiY = ImageProperties.DpiY,
                Data = data
            });
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);

        // підписуємось на зміни ImageProperties і Color
        ImageProperties.Changed.Subscribe(_ => UpdateLivePreview());
        this.WhenAnyValue(x => x.BackgroundColor).Subscribe(_ => UpdateLivePreview());
        UpdateLivePreview();
    }

    private void UpdateLivePreview()
    {
        // беремо базовий LivePreviewParams з ImageProperties і додаємо колір
        var base_ = ImageProperties.LivePreviewParams;
        LivePreviewParams = new PixelModel
        {
            Width = base_.Width,
            Height = base_.Height,
            Mode = base_.Mode,
            BitDepth = base_.BitDepth,
            ColorSpace = base_.ColorSpace,
            Alpha = base_.Alpha,
            DpiX = base_.DpiX,
            DpiY = base_.DpiY,
            BigEndian = base_.BigEndian,
            // дані пікселів з фоновим кольором для preview
            Data = PixelModelService.CreateRgba32(base_.Width, base_.Height, BackgroundColor)
        };

        _layer = new LayerModel(
            LivePreviewParams.Width, 
            LivePreviewParams.Height, 
            BitmapService.RGBAToBGRA(LivePreviewParams.Data), 
            "Preview Layer");
    }
}