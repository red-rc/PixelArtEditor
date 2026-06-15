using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.UI;
using PixelArtEditor.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

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

        LayerListBox.SelectionChanged += (_, e) =>
        {
            _vm.SelectedLayers = new ObservableCollection<LayerItemVM>(
                LayerListBox.SelectedItems?.OfType<LayerItemVM>() ?? []);
        };

        LayerManagerProperty.Changed.AddClassHandler<LayerPanel>((sender, _) =>
        {
            sender._vm?.SetLayerManager(sender.LayerManager);
        });
    }
}
