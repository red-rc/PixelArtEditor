using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Views.EditorControls;

public partial class LayerPanel : UserControl
{
    public static readonly StyledProperty<LayerManager> LayerManagerProperty =
        AvaloniaProperty.Register<LayerPanel, LayerManager>(nameof(LayerManager));

    public LayerManager LayerManager
    {
        get => GetValue(LayerManagerProperty);
        set => SetValue(LayerManagerProperty, value);
    }

    public LayerPanel()
    {
        InitializeComponent();
        DataContext = new LayerPanelVM(LayerManager);
    }
}
