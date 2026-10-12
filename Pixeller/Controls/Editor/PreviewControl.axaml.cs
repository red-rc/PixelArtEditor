using Avalonia;
using Avalonia.Controls;
using Pixeller.Models;
using Pixeller.UI;

namespace Pixeller.Controls.Editor;

public partial class PreviewControl : UserControl
{
    public static readonly StyledProperty<double?> FixedTileSizeProperty =
        AvaloniaProperty.Register<Checkerboard, double?>(nameof(FixedTileSize));

    public double? FixedTileSize
    {
        get => GetValue(FixedTileSizeProperty);
        set => SetValue(FixedTileSizeProperty, value);
    }

    public static readonly StyledProperty<float> CheckerboardOpacityProperty =
        AvaloniaProperty.Register<PreviewControl, float>(nameof(CheckerboardOpacity), defaultValue: 1f);

    public float CheckerboardOpacity
    {
        get => GetValue(CheckerboardOpacityProperty);
        set => SetValue(CheckerboardOpacityProperty, value);
    }

    public static readonly StyledProperty<PreviewData> RenderDataProperty =
        AvaloniaProperty.Register<PreviewControl, PreviewData>(nameof(RenderData));

    public PreviewData RenderData
    {
        get => GetValue(RenderDataProperty);
        set => SetValue(RenderDataProperty, value);
    }

    public PreviewControl() => InitializeComponent();
}