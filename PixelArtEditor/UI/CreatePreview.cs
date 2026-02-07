using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.UI;

public class CreatePreview : Control
{
    public static readonly StyledProperty<CreateParams> ParametersProperty =
        AvaloniaProperty.Register<CreatePreview, CreateParams>(nameof(Parameters));

    public CreateParams Parameters
    {
        get => GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }
    
    private ImageBrush? _checkerboardBrush;
    
    public CreatePreview()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        ParametersProperty.Changed.AddClassHandler<CreatePreview>((sender, _) => sender.InvalidateVisual());
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Parameters is not { Width: > 0, Height: > 0 }) return;

        var ratio = (double)Parameters.Width / Parameters.Height;
        if (double.IsInfinity(ratio) || double.IsNaN(ratio)) ratio = 1;

        var rect = 200 / ratio > 200 ? new Rect((200 - 200 * ratio) / 2, 0, 200 * ratio, 200) : 
            new Rect(0, (200 - 200 / ratio) / 2, 200, 200 / ratio);
        
        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(8, 8, BitmapService.CreateZeroPixelData(8, 8)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 64, 64, RelativeUnit.Absolute)
        };
        
        context.FillRectangle(_checkerboardBrush, rect);
        context.FillRectangle(new SolidColorBrush(Parameters.BackgroundColor), rect);
    }
}