using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using SixLabors.ImageSharp.ColorSpaces;

namespace PixelArtEditor.UI;

public class Preview : Control
{
    public static readonly StyledProperty<PixelModel> ParametersProperty =
        AvaloniaProperty.Register<Preview, PixelModel>(nameof(Parameters));

    public PixelModel Parameters
    {
        get => GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    private byte[]? _lastPixelModelData;
    private bool ConvertToBgra;

    private ImageBrush? _checkerboardBrush;
    private WriteableBitmap? _renderBitmap;
    
    public Preview()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        ParametersProperty.Changed.AddClassHandler<Preview>((sender, _) => sender.InvalidateVisual());
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Parameters is not { Width: > 0, Height: > 0 }) return;

        var ratio = (double)Parameters.Width / Parameters.Height;
        if (double.IsInfinity(ratio) || double.IsNaN(ratio)) ratio = 1;

        var rect = 200 / ratio > 200 ? new Rect((200 - 200 * ratio) / 2, 0, 200 * ratio, 200) : 
            new Rect(0, (200 - 200 / ratio) / 2, 200, 200 / ratio);
        
        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(8, 8, BitmapService.CreateCheckerBoardPixelData(8, 8)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 64, 64, RelativeUnit.Absolute)
        };

        byte[] pixelData;
        if ((Services.ImageData?.BitmapPixelData?.Length ?? 0) > 0)
        {
            pixelData = Services.ImageData!.BitmapPixelData!;
        }
        else
        {
            pixelData = Parameters.Data ?? [];
            ConvertToBgra = true;
        }

        // перестворюємо якщо розмір змінився або bitmap ще не створений
        if (_renderBitmap is null
            || _renderBitmap.PixelSize.Width != Parameters.Width
            || _renderBitmap.PixelSize.Height != Parameters.Height
            || !ReferenceEquals(_lastPixelModelData, pixelData))
        {
            _renderBitmap?.Dispose();

            var expectedSize = Parameters.Width * Parameters.Height * 4;
            byte[] data;

            if (pixelData.Length == expectedSize)
            {
                if (ConvertToBgra)
                {
                    data = new byte[pixelData.Length];
                    for (var i = 0; i < pixelData.Length; i += 4)
                    {
                        data[i + 0] = pixelData[i + 2]; // B ← R
                        data[i + 1] = pixelData[i + 1]; // G
                        data[i + 2] = pixelData[i + 0]; // R ← B
                        data[i + 3] = pixelData[i + 3]; // A
                    }
                }
                else
                {
                    data = pixelData;
                }
            }
            else
            {
                var oldWidth = Services.ImageData?.Model?.Width ?? Parameters.Width;
                var oldHeight = Services.ImageData?.Model?.Height ?? Parameters.Height;

                data = BitmapService.ResizePixelData(
                    pixelData,
                    oldWidth, oldHeight,
                    Parameters.Width, Parameters.Height);
            }

            _renderBitmap = BitmapService.CreateBitmap(Parameters.Width, Parameters.Height, data);
        }

        _lastPixelModelData = pixelData;

        context.FillRectangle(_checkerboardBrush, rect);
        context.DrawImage(_renderBitmap, rect);
    }
}