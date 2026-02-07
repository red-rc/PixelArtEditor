using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.UI;

public class ExportPreview : Control
{
    public static readonly StyledProperty<IPreviewParams> ParametersProperty =
    AvaloniaProperty.Register<ExportPreview, IPreviewParams>(nameof(Parameters));

    public IPreviewParams Parameters
    {
        get => GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    private WriteableBitmap? _previewBitmap;
    private ImageBrush? _checkerboardBrush;

    public ExportPreview()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        ParametersProperty.Changed.AddClassHandler<ExportPreview>((sender, e) =>
        {
            if (sender.Parameters is ExportParams exportParams)
            {
                sender._previewBitmap = Services.ExportPreview.GetUpdatedPreview(exportParams);
            }  
            else if (sender.Parameters is PropertiesParams propertiesParams)
            {
                sender._previewBitmap = Services.ExportPreview.PreviewBitmap;
                if (sender._previewBitmap is null) return;
                BitmapService.UpdateBitmapProperties(ref sender._previewBitmap, propertiesParams.Width, propertiesParams.Height, new Vector(96, 96), AlphaFormat.Unpremul);
            }
               
            sender.InvalidateVisual();
        });
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Parameters is not { Width: > 0, Height: > 0 } || _previewBitmap is null) return;

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
        context.DrawImage(_previewBitmap, new Rect(0, 0, Parameters.Width, Parameters.Height), rect);
    }
}
