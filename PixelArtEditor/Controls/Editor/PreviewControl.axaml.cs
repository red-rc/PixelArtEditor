using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.Models;
using PixelArtEditor.UI;

namespace PixelArtEditor.Controls.Editor;

public partial class PreviewControl : UserControl
{
    public static readonly StyledProperty<double?> FixedTileSizeProperty =
        AvaloniaProperty.Register<Checkerboard, double?>(nameof(FixedTileSize));

    public double? FixedTileSize
    {
        get => GetValue(FixedTileSizeProperty);
        set => SetValue(FixedTileSizeProperty, value);
    }

    public static readonly StyledProperty<PreviewData> RenderDataProperty =
        AvaloniaProperty.Register<PreviewControl, PreviewData>(nameof(RenderData));

    public PreviewData RenderData
    {
        get => GetValue(RenderDataProperty);
        set => SetValue(RenderDataProperty, value);
    }

    public PreviewControl()
    {
        InitializeComponent();
    }
}