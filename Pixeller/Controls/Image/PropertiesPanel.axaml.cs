using Avalonia;
using Avalonia.Controls;

namespace Pixeller.Controls.Image;

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

    public static readonly StyledProperty<bool> ExportOptionsVisibleProperty =
        AvaloniaProperty.Register<PropertiesPanel, bool>(nameof(ExportOptionsVisible), false);

    /// <summary>Shows the save options (TIFF compression, JPEG/WebP quality); only the export dialog enables it.</summary>
    public bool ExportOptionsVisible
    {
        get => GetValue(ExportOptionsVisibleProperty);
        set => SetValue(ExportOptionsVisibleProperty, value);
    }

    public PropertiesPanel()
    {
        InitializeComponent();
    }
}