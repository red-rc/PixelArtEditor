using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using System;

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

    public PixelModel LivePreviewParams => ImageProperties.LivePreviewParams;

    private LayerModel _layer = null!;
    public LayerModel Layer
    {
        get => _layer;
        private set => this.RaiseAndSetIfChanged(ref _layer, value);
    }

    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }

    public CreateDialogVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        CreateCommand = ReactiveCommand.Create(() =>
        {
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
                Data = ImageProperties.PixelData
            });
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);

        ImageProperties.WhenAnyValue(
            x => x.Width,
            x => x.Height
        )
        .Subscribe(_ =>
        {
            ImageProperties.PixelData = PixelModelService.CreateRgba32(
                ImageProperties.LivePreviewParams.Width,
                ImageProperties.LivePreviewParams.Height,
                BackgroundColor);
            UpdateLayer();
        });

        this.WhenAnyValue(x => x.BackgroundColor).Subscribe(color =>
        {
            ImageProperties.PixelData = PixelModelService.CreateRgba32(
                ImageProperties.LivePreviewParams.Width,
                ImageProperties.LivePreviewParams.Height,
                color);
            UpdateLayer();
        });
    }

    private void UpdateLayer()
    {
        Layer = new LayerModel(
            ImageProperties.LivePreviewParams.Width,
            ImageProperties.LivePreviewParams.Height,
            ImageProperties.LivePreviewParams.Data,
            "Preview Layer");
    }
}