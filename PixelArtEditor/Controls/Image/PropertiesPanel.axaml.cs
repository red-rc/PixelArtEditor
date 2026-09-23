using Avalonia;
using Avalonia.Controls;

namespace PixelArtEditor.Controls.Image;

public partial class PropertiesPanel : UserControl
{
    public static readonly StyledProperty<bool> ConfigureVisibleProperty =
        AvaloniaProperty.Register<PropertiesPanel, bool>(nameof(ConfigureVisible), true);

    public bool ConfigureVisible
    {
        get => GetValue(ConfigureVisibleProperty);
        set => SetValue(ConfigureVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> ConfigureEnabledProperty =
        AvaloniaProperty.Register<PropertiesPanel, bool>(nameof(ConfigureEnabled), true);

    public bool ConfigureEnabled
    {
        get => GetValue(ConfigureEnabledProperty);
        set => SetValue(ConfigureEnabledProperty, value);
    }

    public PropertiesPanel()
    {
        InitializeComponent();
    }
}