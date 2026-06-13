using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Views.EditorControls;

public partial class LayerPanel : UserControl
{
    private readonly LayerPanelVM? _vm;

    public static readonly StyledProperty<LayerManager> LayerManagerProperty =
        AvaloniaProperty.Register<LayerPanel, LayerManager>(nameof(LayerManager));

    public LayerManager LayerManager
    {
        get => GetValue(LayerManagerProperty);
        set => SetValue(LayerManagerProperty, value);
    }

    public LayerPanel()
    {
        _vm = new LayerPanelVM();
        DataContext = _vm;

        InitializeComponent();

        LayerManagerProperty.Changed.AddClassHandler<LayerPanel>((sender, _) =>
        {
            sender._vm?.SetLayerManager(sender.LayerManager);
        });
    }
}
