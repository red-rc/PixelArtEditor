using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.ViewModels;

public class CreateDialogVM : ReactiveObject
{
    private Color _backgroundColor = Colors.White;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set 
        {
            this.RaiseAndSetIfChanged(ref _backgroundColor, value);
            PushRenderData();
        }
    }

    private PreviewData _renderData = new(0, 0, null, null);
    public PreviewData RenderData
    {
        get => _renderData;
        private set => this.RaiseAndSetIfChanged(ref _renderData, value);
    }

    private void PushRenderData()
        => RenderData = new(ImageProperties.Width, ImageProperties.Height, null, BackgroundColor);

    public ImagePropertiesUCVM ImageProperties { get; }

    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }

    public CreateDialogVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        CreateCommand = ReactiveCommand.Create(() =>
        {
            ImageProperties.PixelData = PixelModelService.CreateRgba32(
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
                Data = ImageProperties.PixelData
            });
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);

        ImageProperties.WhenAnyValue(x => x.Width, x => x.Height).Subscribe(_ =>
        {
            PushRenderData();
        });
    }
}