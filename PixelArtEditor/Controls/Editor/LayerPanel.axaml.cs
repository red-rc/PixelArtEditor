using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.EditorUI.LayerPanel;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.UI;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace PixelArtEditor.Controls.Editor;

public partial class LayerPanel : UserControl, ILayerPanelContext
{
    private readonly LayerPanelVM? _vm;
    public LayerPanelVM ViewModel => _vm!;
    public ICanvasContext? GetCanvasContext() =>
        Services.Navigation.GetViewModel() is EditorVM vm ? vm.Canvas : null;

    public static readonly StyledProperty<LayerManager?> LayerManagerProperty =
        AvaloniaProperty.Register<LayerPanel, LayerManager?>(nameof(LayerManager));

    public LayerManager? LayerManager
    {
        get => GetValue(LayerManagerProperty);
        set => SetValue(LayerManagerProperty, value);
    }

    private Point _mousePressPos;
    private bool _dragging;

    public readonly LayerCmdList LayerCommands;
    private readonly LayerDnDManager _dndManager;

    private void AddClick(object? sender, RoutedEventArgs e) => LayerCommands.AddCmd.Execute(LayerManager);
    public void DeleteClick(object? sender, RoutedEventArgs e) => LayerCommands.DeleteCmd.Execute(LayerManager);
    public void DuplicateClick(object? sender, RoutedEventArgs e) => LayerCommands.DuplicateCmd.Execute(LayerManager);
    public void GroupClick(object? sender, RoutedEventArgs e) => LayerCommands.GroupCmd.Execute(LayerManager);

    private void ToTheTopClick(object? sender, RoutedEventArgs e) => LayerCommands.MoveCmd.Execute(LayerManager, true);
    private void ToTheBottomClick(object? sender, RoutedEventArgs e) => LayerCommands.MoveCmd.Execute(LayerManager, false);

    public void RenameClick(object? sender, RoutedEventArgs e) => ShowAndFocusTextBox();

    public LayerModel? GetActiveLayer() => LayerManager?.ActiveLayer;
    public ObservableCollection<LayerModel>? GetLayers() => LayerManager?.Layers;
    public ObservableCollection<LayerModel> GetSelLayers() 
    {
        return new ObservableCollection<LayerModel>(
            _vm?.SelLayerItems?
                .OrderBy(LayerListBox.Items.IndexOf)
                .Select(x => x.Layer)
                .Where(x => x != null)!
        );
    }

    public LayerPanel()
    {
        _vm = new LayerPanelVM();
        DataContext = _vm;
        InitializeComponent();

        LayerCommands = new LayerCmdList(_vm, LayerListBox);
        _dndManager = new LayerDnDManager(LayerListBox, FloatingHost, CountBadge, CountBadgeText);

        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        editorVM.WhenAnyValue(e => e.IsTransforming).Subscribe(isTransforming =>
        {
            for (var i = 0; i < LayerListBox.ItemCount; i++)
            {
                if (LayerListBox.ContainerFromIndex(i) is ListBoxItem item)
                    item.Classes.Set("disabled", isTransforming);
            }
        });

        LayerListBox.AddHandler(PointerPressedEvent, OnItemPointerPressed, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerMovedEvent, OnItemPointerMoved, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerReleasedEvent, OnItemPointerReleased, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerPressedEvent, OnLockButtonPointerPressed, RoutingStrategies.Tunnel);

        LayerListBox.SelectionChanged += OnSelectionChanged;
    }

    private void LayerListBox_PointerPressed(object? sender, PointerPressedEventArgs e) => LayerListBox.SelectedItems?.Clear();
    
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_vm is null) return;

        _vm.SelLayerItems = new ObservableCollection<LayerItem>(
            LayerListBox.SelectedItems?.OfType<LayerItem>() ?? []);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayerManagerProperty)
        {
            _vm?.SetLayerManager(LayerManager);
            _dndManager.LayerManager = LayerManager;
        }
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Services.Navigation.GetViewModel() is EditorVM editorVM && editorVM.IsTransforming)
        {
            e.Handled = true;
            return;
        }

        if (!(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            || _dragging
            || LayerManager?.Layers.Count <= 1
            || (e.Source as Control)?.FindAncestorOfType<ScrollBar>() is not null) return;

        var pressedListBoxItem = (e.Source as Control)?.FindAncestorOfType<ListBoxItem>();

        if (pressedListBoxItem is not null
            && e.GetCurrentPoint(this).Properties.IsRightButtonPressed
            && LayerListBox.SelectedItems?.Count < 2)
        {
            LayerListBox.SelectedItems?.Clear();
            pressedListBoxItem.IsSelected = true;
        }

        var selected = LayerListBox.SelectedItems?
                .Cast<LayerItem>()
                .OrderBy(LayerListBox.Items.IndexOf)
                .Select(item => LayerListBox.ContainerFromItem(item) as ListBoxItem)
                .OfType<ListBoxItem>()
                .ToList() ?? [];

        if (pressedListBoxItem is not null && !selected.Contains(pressedListBoxItem))
            _dndManager.DraggedItems = [pressedListBoxItem];
        else
            _dndManager.DraggedItems = selected;

        _mousePressPos = e.GetPosition(this);
        _dndManager.ItemHeight = (int)(_dndManager.DraggedItems.FirstOrDefault()?.Bounds.Height ?? 0);
    }

    private void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || LayerManager is null
            || LayerManager.Layers.Count <= 1
            || _dndManager.DraggedItems.Count == 0
            || Services.Navigation.GetViewModel() is not EditorVM editorVM
            || editorVM.IsTransforming
            || (e.Source as Control)?.FindAncestorOfType<ScrollBar>() is not null) return;

        var dx = e.GetPosition(this).X - _mousePressPos.X;
        var dy = e.GetPosition(this).Y - _mousePressPos.Y;

        if (!_dragging)
        {
            if (dx * dx + dy * dy < 100) return;
            _dndManager.StartDragVisual();
        }

        _dragging = true;

        if (_dndManager.DraggedItems.Count > 3)
        {
            CountBadge.IsVisible = true;
            CountBadgeText.Text = $"{_dndManager.DraggedItems.Count} layers";
            Avalonia.Controls.Canvas.SetLeft(CountBadge, e.GetPosition(this).X + 5);
            Avalonia.Controls.Canvas.SetTop(CountBadge, e.GetPosition(this).Y + 5);
        }

        _dndManager.AutoScrollIfNeeded(e);
        var target = _dndManager.GetTargetIndex(e);

        if (target != _dndManager.TargetIndex)
        {
            _dndManager.TargetIndex = target;
            _dndManager.AnimateItems();
        }

        if (FloatingHost.Children.Count > 0)
        {
            for (var i = 0; i < FloatingHost.Children.Count; i++)
            {
                var top = Math.Clamp(
                    e.GetPosition(FloatingHost).Y + i * _dndManager.ItemHeight, 
                    i * _dndManager.ItemHeight, 
                    LayerListBox.Bounds.Height + i * _dndManager.ItemHeight);
                Avalonia.Controls.Canvas.SetTop(FloatingHost.Children[i], top);
            }
        }
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        _dndManager.ResetItemsTransform();

        if (_dndManager.TargetIndex.HasValue && _dndManager.DraggedItems.Count > 0 && LayerManager is not null)
            _dndManager.MoveGroupTo(_dndManager.TargetIndex.Value);

        _dndManager.CleanupDrag();
    }

    private void OnLockButtonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control source) return;

        var toggleButton = source.FindAncestorOfType<InstantToggleButton>(includeSelf: true);
        if (toggleButton is not { Tag: "Lock/Unlock" } || toggleButton.DataContext is not LayerItem clickedItem) return;

        var newValue = !clickedItem.IsLocked;

        if (_vm?.SelLayerItems is { Count: > 0 } sel && sel.Any(i => i == clickedItem))
        {
            foreach (var item in sel)
                item.IsLocked = newValue;
        }
        else
            clickedItem.IsLocked = newValue;

        e.Handled = true;
    }

    private void TextBlock_DoubleTapped(object? sender, TappedEventArgs e) => ShowAndFocusTextBox(sender);

    private void ShowAndFocusTextBox(object? sender = null)
    {
        TextBlock? textBlock;

        if (sender is not null)
            textBlock = (TextBlock)sender;
        else if (_vm?.SelLayerItem is not null)
        {
            var container = LayerListBox.ContainerFromItem(_vm.SelLayerItem);
            textBlock = container?.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        }
        else
            return;

        if (textBlock is null) return;

        var grid = textBlock.FindAncestorOfType<Grid>();
        var textBox = grid?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "RenameTextBox");

        (textBlock.DataContext as LayerItem)?.IsEditing = true;
        Dispatcher.UIThread.Post(() =>
        {
            textBox?.Focus();
            textBox?.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    private void RenameTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        var textBox = (TextBox)sender!;
        var item = textBox.DataContext as LayerItem;

        if (e.Key == Key.Enter)
            item?.IsEditing = false;
        else if (e.Key == Key.Escape)
        {
            item?.LayerName = "";
            item?.IsEditing = false;
        }
    }

    private void RenameTextBox_LostFocus(object? sender, FocusChangedEventArgs e)
    {
        var textBox = (TextBox)sender!;
        (textBox.DataContext as LayerItem)?.IsEditing = false;
    }

    private void LayerItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint((Visual)sender!).Properties.IsRightButtonPressed
            || sender is not Control root) return;

        if (root.FindAncestorOfType<InstantToggleButton>(includeSelf: true) is not null ||
            root.FindAncestorOfType<Preview>(includeSelf: true) is not null) return;

        var flyout = new Flyout
        {
            Placement = PlacementMode.Pointer,
            ShowMode = FlyoutShowMode.Standard,
            VerticalOffset = -6
        };

        flyout.Content = new LayerContextMenu(this, flyout.Hide);

        flyout.Popup.Opened += (_, _) =>
        {
            if (flyout.Popup.Child is not FlyoutPresenter presenter) return;

            var content = presenter.GetVisualDescendants().OfType<LayerContextMenu>().FirstOrDefault();
            if (content is null) return;

            var popupScreenPos = content.PointToScreen(new Point(0, 0));
            var clickScreenPos = this.PointToScreen(e.GetPosition(this));

            var dx = popupScreenPos.X < clickScreenPos.X ? 6 : -6;

            flyout.Popup.HorizontalOffset = dx;
        };

        flyout.ShowAt(root, showAtPointer: true);
        e.Handled = true;
    }
}