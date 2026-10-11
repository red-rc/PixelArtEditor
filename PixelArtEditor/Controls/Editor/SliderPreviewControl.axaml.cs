using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.Models;

namespace PixelArtEditor.Controls.Editor;

public partial class SliderPreviewControl : UserControl
{
    public static readonly StyledProperty<PreviewData> RenderDataProperty =
        AvaloniaProperty.Register<PreviewControl, PreviewData>(nameof(RenderData));

    public PreviewData RenderData
    {
        get => GetValue(RenderDataProperty);
        set => SetValue(RenderDataProperty, value);
    }

    public static readonly StyledProperty<float> CheckerboardOpacityProperty =
        AvaloniaProperty.Register<PreviewControl, float>(nameof(CheckerboardOpacity), defaultValue: 1f);

    public float CheckerboardOpacity
    {
        get => GetValue(CheckerboardOpacityProperty);
        set => SetValue(CheckerboardOpacityProperty, value);
    }

    public SliderPreviewControl() => InitializeComponent();
}