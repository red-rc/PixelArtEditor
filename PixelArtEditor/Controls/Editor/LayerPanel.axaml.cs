using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.EditorUI;
using PixelArtEditor.AppServices.EditorUI.LayerCommands;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PixelArtEditor.Controls.Editor;

public partial class LayerPanel : UserControl
{
    private readonly LayerPanelVM? _vm;
    public LayerPanelVM ViewModel => _vm!;

    public static readonly StyledProperty<LayerManager?> LayerManagerProperty =
        AvaloniaProperty.Register<LayerPanel, LayerManager?>(nameof(LayerManager));

    public LayerManager? LayerManager
    {
        get => GetValue(LayerManagerProperty);
        set => SetValue(LayerManagerProperty, value);
    }

    private Point _mousePressPos;
    private bool _dragging;

    public readonly LayerCommands LayerCommands;
    private readonly DnDManager _dndManager;

    private void OnAddClick(object? sender, RoutedEventArgs e) => LayerCommands.AddCommand.Execute(LayerManager);
    private void OnRemoveClick(object? sender, RoutedEventArgs e) => LayerCommands.RemoveCommand.Execute(LayerManager);
    private void OnDuplicateClick(object? sender, RoutedEventArgs e) => LayerCommands.DuplicateCommand.Execute(LayerManager);
    private void OnGroupClick(object? sender, RoutedEventArgs e) => LayerCommands.GroupCommand.Execute(LayerManager);

    private void OnToTheTopClick(object? sender, RoutedEventArgs e) => LayerCommands.MoveCommand.Execute(LayerManager, true);
    private void OnToTheBottomClick(object? sender, RoutedEventArgs e) => LayerCommands.MoveCommand.Execute(LayerManager, false);
    public void OnUpClick(object? sender, RoutedEventArgs e) => LayerCommands.MoveStepCommand.Execute(LayerManager, -1);
    public void OnDownClick(object? sender, RoutedEventArgs e) => LayerCommands.MoveStepCommand.Execute(LayerManager, 1);

    public LayerPanel()
    {
        _vm = new LayerPanelVM();
        DataContext = _vm;
        InitializeComponent();

        LayerCommands = new LayerCommands(_vm, LayerListBox);
        _dndManager = new DnDManager(LayerListBox, FloatingHost, CountBadge, CountBadgeText);

        LayerListBox.AddHandler(PointerPressedEvent, OnItemPointerPressed, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerMovedEvent, OnItemPointerMoved, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerReleasedEvent, OnItemPointerReleased, RoutingStrategies.Tunnel);
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
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || _dragging || LayerManager?.Layers.Count <= 1) return;

        var pressedListBoxItem = (e.Source as Control)?.FindAncestorOfType<ListBoxItem>();

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
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || LayerManager is null
            || LayerManager.Layers.Count <= 1 || _dndManager.DraggedItems.Count == 0) return;

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

    private void TextBlock_DoubleTapped(object? sender, TappedEventArgs e)
    {
        var textBlock = (TextBlock)sender!;
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
}
