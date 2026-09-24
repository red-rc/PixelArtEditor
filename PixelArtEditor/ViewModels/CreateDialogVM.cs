using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.AppServices.ImageProcessing;
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
    {
        RenderData.Width = ImageProperties.Width;
        RenderData.Height = ImageProperties.Height;
        RenderData.Color = BackgroundColor;
        RenderData.NotifyPropertyChanged();
    }

    public ImagePropertiesUCVM ImageProperties { get; }

    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }

    public CreateDialogVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        CreateCommand = ReactiveCommand.Create(() =>
        {
            var data = new byte[ImageProperties.Width * ImageProperties.Height * 4];
            for (var i = 0; i < data.Length; i += 4)
            {
                data[i + 0] = BackgroundColor.B;
                data[i + 1] = BackgroundColor.G;
                data[i + 2] = BackgroundColor.R;
                data[i + 3] = BackgroundColor.A;
            }

            dialog.Close(new PixelModel
            {
                Width = ImageProperties.Width,
                Height = ImageProperties.Height,
                Mode = ImageProperties.ColorMode,
                BitDepth = ImageProperties.BitDepth,
                ColorSpace = ImageProperties.ColorSpace,
                Alpha = ImageProperties.AlphaFormat,
                DpiX = ImageProperties.DpiX,
                DpiY = ImageProperties.DpiY,
                Data = data
            });
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);

        ImageProperties.WhenAnyValue(x => x.Width, x => x.Height).Subscribe(_ =>
        {
            PushRenderData();
        });
    }
}