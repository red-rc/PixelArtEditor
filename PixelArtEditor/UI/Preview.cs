using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models;
using System;
using System.Reactive.Linq;

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

    public Preview()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        this.WhenAnyValue(x => x.RenderData)
            .Select(model => model.WhenAnyValue(rd => rd.Width, rd => rd.Height, rd => rd.Bitmap, rd => rd.Color))
            .Switch()
            .Subscribe(_ => InvalidateVisual());
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var data = RenderData;

        if (data is null || Bounds.Width < 0 || Bounds.Height < 0) return;

        if (Bounds.Width != Bounds.Height)
            throw new InvalidOperationException(LocalizationService.Get("PreviewSquareError"));

        var bitmap = data.Bitmap;
        Color? color = data.Color;

        if (bitmap is null && color is null) return;

        var rect = PreviewHelper.GetAspectRect(Bounds.Width, data.Width, data.Height);

        if (bitmap is not null)
            context.DrawImage(bitmap, rect);
        else if (color is Color c)
            context.DrawRectangle(new SolidColorBrush(c), null, rect);
    }
}