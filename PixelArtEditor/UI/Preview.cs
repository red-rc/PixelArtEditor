using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models;

namespace PixelArtEditor.UI;

public class Preview : Control
{
    public static readonly StyledProperty<PreviewData> RenderDataProperty =
        AvaloniaProperty.Register<Preview, PreviewData>(nameof(RenderData), defaultValue: new PreviewData(0, 0, null, null));

    public PreviewData RenderData
    {
        get => GetValue(RenderDataProperty);
        set => SetValue(RenderDataProperty, value);
    }

    private ImageBrush? _checkerboardBrush;
    private WriteableBitmap? _renderBitmap;

    private readonly BoxShadows _shadow = Application.Current?.Resources["CardShadow"] as BoxShadows? ?? default;

    public Preview()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        RenderDataProperty.Changed.AddClassHandler<Preview>((sender, _) => OnRenderDataChanged());
    }

    private void OnRenderDataChanged()
    {
        var data = RenderData;
        if (data is null || data.Width < 1 || data.Height < 1) return;

        if (RenderData.PixelData is not null)
            OnPixelDataChanged();
        else if (RenderData.Color is not null)
            OnColorChanged();
        else
            return;
    }

    private void OnPixelDataChanged()
    {
        if (RenderData.PixelData is not { Length: > 0 } pixelData) return;

        _renderBitmap?.Dispose();
        _renderBitmap = BitmapService.CreateBitmap(RenderData.Width, RenderData.Height, pixelData);

        InvalidateVisual();
    }

    private void OnColorChanged()
    {
        if (RenderData.Color is not Color color) return;

        _renderBitmap?.Dispose();
        _renderBitmap = BitmapService.CreateBitmap(1, 1, color);

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_renderBitmap is null || Width < 0 || Height < 0) return;

        var ratio = (double)RenderData.Width / RenderData.Height;
        if (double.IsInfinity(ratio) || double.IsNaN(ratio)) ratio = 1;

        var rect = Width / ratio > Width
            ? new Rect((int)((double)(Width - Width * ratio) / 2), 0, (int)(Width * ratio), Width)
            : new Rect(0, (int)((double)(Width - Width / ratio) / 2), Width, (int)(Width / ratio));
        
        _checkerboardBrush ??= new ImageBrush(BitmapService.CreateBitmap(2, 2, BitmapService.CreateCheckerBoardPixelData(2, 2)))
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            DestinationRect = new RelativeRect(0, 0, 20, 20, RelativeUnit.Absolute)
        };

        context.DrawRectangle(null, null, rect, 0, 0, _shadow);
        context.FillRectangle(_checkerboardBrush, rect);
        context.DrawImage(_renderBitmap, rect);
    }
}