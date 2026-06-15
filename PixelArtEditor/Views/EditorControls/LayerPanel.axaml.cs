using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace PixelArtEditor.Views.EditorControls;

public partial class LayerPanel : UserControl
{
    private readonly LayerPanelVM? _vm;

    public static readonly StyledProperty<LayerManager?> LayerManagerProperty =
        AvaloniaProperty.Register<LayerPanel, LayerManager?>(nameof(LayerManager));

    public LayerManager? LayerManager
    {
        get => GetValue(LayerManagerProperty);
        set => SetValue(LayerManagerProperty, value);
    }

    public LayerPanel()
    {
        _vm = new LayerPanelVM();
        DataContext = _vm;
        InitializeComponent();

        LayerListBox.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_vm is null) return;

        _vm.SelectedLayers = new ObservableCollection<LayerItemVM>(
            LayerListBox.SelectedItems?.OfType<LayerItemVM>() ?? []);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayerManagerProperty)
            _vm?.SetLayerManager(LayerManager);
    }
}
