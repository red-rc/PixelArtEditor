using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.Models;
using PixelArtEditor.Models.Canvas;
using System;
using System.Runtime.InteropServices;

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
            if (BackgroundColor != Colors.Transparent)
            {
                var packed = (uint)(
                    BackgroundColor.B | 
                    BackgroundColor.G << 8 | 
                    BackgroundColor.R << 16 | 
                    BackgroundColor.A << 24);

                MemoryMarshal.Cast<byte, uint>(data.AsSpan()).Fill(packed);
            }

            dialog.Close(new PixelModel
            {
                Width = ImageProperties.Width,
                Height = ImageProperties.Height,
                ColorMode = ImageProperties.ColorMode,
                BitDepth = ImageProperties.BitDepth,
                ColorSpace = ImageProperties.ColorSpace,
                AlphaFormat = ImageProperties.AlphaFormat,
                DpiX = ImageProperties.DpiX,
                DpiY = ImageProperties.DpiY,
                Data = data
            });
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);

        ImageProperties.WhenAnyValue(x => x.Width, x => x.Height).Subscribe(_ => PushRenderData());
    }
}