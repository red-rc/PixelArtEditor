using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.EditorUI;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.LayerPanel;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.Generic;
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
    private int _itemHeight;

    private List<ListBoxItem> _draggedItems = [];
    private int? _targetIndex;

    private readonly LayerReorderService _layerReorderService;

    private ScrollViewer? _scrollViewer;

    private void OnToTheTopClick(object? sender, RoutedEventArgs e) => _layerReorderService.MoveSelected(LayerManager, true);
    private void OnToTheBottomClick(object? sender, RoutedEventArgs e) => _layerReorderService.MoveSelected(LayerManager, false);
    public void OnUpClick(object? sender, RoutedEventArgs e) => _layerReorderService.MoveSelectedStep(LayerManager, -1);
    public void OnDownClick(object? sender, RoutedEventArgs e) => _layerReorderService.MoveSelectedStep(LayerManager, 1);

    public LayerPanel()
    {
        _vm = new LayerPanelVM();
        DataContext = _vm;
        InitializeComponent();

        _layerReorderService = new LayerReorderService(_vm, LayerListBox);

        LayerListBox.AddHandler(PointerPressedEvent, OnItemPointerPressed, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerMovedEvent, OnItemPointerMoved, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerReleasedEvent, OnItemPointerReleased, RoutingStrategies.Tunnel);
        LayerListBox.SelectionChanged += OnSelectionChanged;
    }

    private IEnumerable<LayerItem> DraggedLayerItems =>
        _draggedItems.Select(d => d.DataContext).OfType<LayerItem>();
    private ScrollViewer? GetScrollViewer() =>
        _scrollViewer ??= LayerListBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

    private void LayerListBox_PointerPressed(object? sender, PointerPressedEventArgs e) => LayerListBox.SelectedItems?.Clear();
    
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_vm is null) return;

        _vm.SelectedLayers = new ObservableCollection<LayerItem>(
            LayerListBox.SelectedItems?.OfType<LayerItem>() ?? []);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayerManagerProperty)
            _vm?.SetLayerManager(LayerManager);
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || _dragging || LayerManager?.Layers.Count <= 1) return;

        var pressedListBoxItem = (e.Source as Control)?.FindAncestorOfType<ListBoxItem>();
        
        var selected = LayerListBox.SelectedItems?
                .Cast<LayerItem>()
                .Select(item => LayerListBox.ContainerFromItem(item) as ListBoxItem)
                .OfType<ListBoxItem>()
                .ToList() ?? [];

        if (pressedListBoxItem is not null && !selected.Contains(pressedListBoxItem))
            _draggedItems = [pressedListBoxItem];
        else
            _draggedItems = selected;

        _mousePressPos = e.GetPosition(this);
        _itemHeight = (int)(_draggedItems.FirstOrDefault()?.Bounds.Height ?? 0);
    }

    private void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || LayerManager?.Layers.Count <= 1 || _draggedItems.Count <= 0) return;

        var dx = e.GetPosition(this).X - _mousePressPos.X;
        var dy = e.GetPosition(this).Y - _mousePressPos.Y;

        if (!_dragging)
        {
            if (dx * dx + dy * dy < 100) return;
            StartDragVisual();
        }

        _dragging = true;

        if (_draggedItems.Count > 3)
        {
            CountBadge.IsVisible = true;
            CountBadgeText.Text = $"{_draggedItems.Count} layers";
            Avalonia.Controls.Canvas.SetLeft(CountBadge, e.GetPosition(this).X + 5);
            Avalonia.Controls.Canvas.SetTop(CountBadge, e.GetPosition(this).Y + 5);
        }

        AutoScrollIfNeeded(e);
        var target = GetTargetIndex(e);

        if (target != _targetIndex)
        {
            _targetIndex = target;
            AnimateItems();
        }

        if (FloatingHost.Children.Count > 0)
        {
            for (var i = 0; i < FloatingHost.Children.Count; i++)
            {
                var top = Math.Clamp(
                    e.GetPosition(FloatingHost).Y + i * _itemHeight, 
                    i * _itemHeight, 
                    LayerListBox.Bounds.Height - (FloatingHost.Children.Count - 1 - i) * _itemHeight);
                Avalonia.Controls.Canvas.SetTop(FloatingHost.Children[i], top);
            }
        }
    }

    private void StartDragVisual()
    {
        foreach (var item in _draggedItems)
                item.Opacity = 0;

        var stackCount = _draggedItems.Count > 3 ? 1 : _draggedItems.Count;

        for (var i = 0; i < stackCount; i++)
        {
            var item = _draggedItems[i].DataContext as LayerItem;

            var preview = new ContentPresenter
            {
                Content = item,
                ContentTemplate = LayerListBox.ItemTemplate,
                Opacity = 0.85,
                ZIndex = -i
            };

            FloatingHost.Children.Add(preview);
        }
    }

    private void AutoScrollIfNeeded(PointerEventArgs e)
    {
        var scrollViewer = GetScrollViewer();
        if (scrollViewer is null) return;

        var pos = e.GetPosition(LayerListBox);
        const double edge = 20;
        const double speed = 5;

        if (pos.Y < edge)
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, scrollViewer.Offset.Y - speed));
        else if (pos.Y > LayerListBox.Bounds.Height - edge)
        {
            var offsetY = Math.Min(scrollViewer.Extent.Height - scrollViewer.Viewport.Height, scrollViewer.Offset.Y + speed);
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, offsetY);
        }
    }

    private int GetTargetIndex(PointerEventArgs e)
    {
        if (_itemHeight <= 0 || LayerManager is null) return 0;

        var scrollOffset = GetScrollViewer()?.Offset.Y ?? 0;
        var y = e.GetPosition(LayerListBox).Y + scrollOffset;

        var nonSelectedIndex = 0;
        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.Items[i] is not LayerItem item || _draggedItems.Any(d => d.DataContext == item)) continue;

            if (y < i * _itemHeight + _itemHeight / 2.0)
                return nonSelectedIndex;

            nonSelectedIndex++;
        }

        return nonSelectedIndex;
    }

    private void AnimateItems()
    {
        if (_targetIndex is null) return;

        var stackCount = _draggedItems.Count > 3 ? 1 : _draggedItems.Count;

        var sourceNonSelectedIndex = 0;
        foreach (var layer in LayerManager!.Layers)
        {
            if (DraggedLayerItems.Any(li => li.Layer == layer)) break;
            sourceNonSelectedIndex++;
        }

        var nonSelectedIndex = 0;

        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.Items[i] is not LayerItem item || DraggedLayerItems.Any(li => li.Layer == item.Layer)) continue;

            if (LayerListBox.ContainerFromIndex(i) is ListBoxItem listBoxItem)
            {
                double targetY = 0;

                if (_targetIndex > sourceNonSelectedIndex && nonSelectedIndex >= sourceNonSelectedIndex
                    && nonSelectedIndex < _targetIndex)
                    targetY = -stackCount * _itemHeight;
                else if (_targetIndex < sourceNonSelectedIndex && nonSelectedIndex < sourceNonSelectedIndex
                    && nonSelectedIndex >= _targetIndex)
                    targetY = stackCount * _itemHeight;

                listBoxItem.RenderTransform = TransformOperations.Parse($"translateY({targetY}px)");
            }

            nonSelectedIndex++;
        }
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        ResetItemsTransform();

        if (_targetIndex.HasValue && _draggedItems.Count > 0 && LayerManager is not null)
            MoveGroupTo(_targetIndex.Value);

        CleanupDrag();
    }

    private void MoveGroupTo(int targetIndex)
    {
        var group = DraggedLayerItems.ToList();
        var layers = LayerManager!.Layers;

        var withoutGroup = layers.Where(l => !group.Any(g => g.Layer == l)).ToList();
        withoutGroup.InsertRange(Math.Clamp(targetIndex, 0, withoutGroup.Count), group.Select(g => g.Layer));

        for (var i = 0; i < withoutGroup.Count; i++)
        {
            var currentIndex = layers.IndexOf(withoutGroup[i]);
            if (currentIndex != i)
                layers.Move(currentIndex, i);
        }

        RestoreSelectionFor();
    }

    private void RestoreSelectionFor()
    {
        LayerListBox.SelectedItems?.Clear();

        foreach (var item in DraggedLayerItems)
            LayerListBox.SelectedItems?.Add(item);
    }

    private void CleanupDrag()
    {
        FloatingHost.Children.Clear();

        foreach (var item in _draggedItems)
            item.Opacity = 1;

        _draggedItems.Clear();
        _targetIndex = null;

        if (CountBadge.IsVisible)
        {
            CountBadge.IsVisible = false;
            CountBadgeText.Text = "0";
        }
    }

    private void ResetItemsTransform()
    {
        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.ContainerFromIndex(i) is ListBoxItem listboxItem)
            {
                var transitions = listboxItem.Transitions;
                listboxItem.Transitions = null;
                listboxItem.RenderTransform = TransformOperations.Identity;
                listboxItem.Transitions = transitions;
            }
        }
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
