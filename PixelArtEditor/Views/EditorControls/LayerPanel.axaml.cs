using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.EditorUI;
using PixelArtEditor.UI;
using PixelArtEditor.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace PixelArtEditor.Views.EditorControls;

public partial class LayerPanel : UserControl
{
    private readonly LayerPanelVM? _vm;
    private readonly TooltipManager _tooltipManager;

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

        _tooltipManager = new TooltipManager(Tooltip, TooltipText, RectHost);
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

    private void Grid_PointerMoved(object? sender, PointerEventArgs e) =>
        _tooltipManager.OnPointerMoved(e, ActionPanel.Children.OfType<Control>()
            .Concat(LayerListBox.GetVisualDescendants().OfType<InstantToggleButton>()));

    private void Grid_PointerExited(object? sender, PointerEventArgs e)
    {
        _tooltipManager.Hide();
    }
}
