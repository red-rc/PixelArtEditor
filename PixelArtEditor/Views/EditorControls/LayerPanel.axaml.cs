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

        LayerManagerProperty.Changed.AddClassHandler<LayerPanel>((sender, _) =>
        {
            if (sender.LayerManager is null) return;
            sender.DataContext = new LayerPanelVM(sender.LayerManager);
        });
    }
}
