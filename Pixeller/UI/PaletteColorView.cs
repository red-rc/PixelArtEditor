using Avalonia;
using Avalonia.Controls;
using Pixeller.Models.Canvas;
using System;

namespace Pixeller.UI;

public class PaletteColorView : ColorView
{
    protected override Type StyleKeyOverride => typeof(ColorView);

    public static readonly StyledProperty<Palette?> ImagePaletteProperty =
        AvaloniaProperty.Register<PaletteColorView, Palette?>(nameof(ImagePalette));

    public Palette? ImagePalette
    {
        get => GetValue(ImagePaletteProperty);
        set => SetValue(ImagePaletteProperty, value);
    }
}