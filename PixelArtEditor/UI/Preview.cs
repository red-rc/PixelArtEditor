using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models;
using System.ComponentModel;

namespace PixelArtEditor.UI;

public class Preview : Control
{
    private static ISettingsManager Settings => Services.Settings;

    public static readonly StyledProperty<PreviewData> RenderDataProperty =
        AvaloniaProperty.Register<Preview, PreviewData>(nameof(RenderData), defaultValue: new PreviewData(0, 0, null, null));

    public PreviewData RenderData
    {
        get => GetValue(RenderDataProperty);
        set => SetValue(RenderDataProperty, value);
    }

    private ImageBrush? _checkerboardBrush;

    private readonly BoxShadows _shadow = Application.Current?.Resources["CardShadow"] as BoxShadows? ?? default;

    public Preview()
    {
        RenderOptions.SetBitmapInterpolationMode(this, Settings.InterpolationMode);

        Settings.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Settings.InterpolationMode))
                RenderOptions.SetBitmapInterpolationMode(this, Settings.InterpolationMode);
        };

        RenderDataProperty.Changed.AddClassHandler<Preview>((sender, e) => OnRenderDataChanged(e));
    }

    private void OnRenderDataChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is PreviewData old) old.PropertyChanged -= OnRenderDataPropertyChanged;
        if (e.NewValue is PreviewData @new) @new.PropertyChanged += OnRenderDataPropertyChanged;
        InvalidateVisual();
    }

    private void OnRenderDataPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var data = RenderData;
        if (data is null || Width < 0 || Height < 0) return;

        var bitmap = data.Bitmap;
        Color? color = data.Color;

        if (bitmap is null && color is null) return;

        var ratio = (double)data.Width / data.Height;
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

        if (bitmap is not null)
            context.DrawImage(bitmap, rect);
        else if (color is Color c)
            context.DrawRectangle(new SolidColorBrush(c), null, rect);
    }
}