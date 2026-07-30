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

namespace PixelArtEditor.Views.EditorControls;

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

    private readonly List<ContentPresenter> _previewPresenters = [];
    private List<LayerModel> _draggedLayers = [];
    private readonly List<ListBoxItem> _draggedItems = [];
    private int? _targetIndex;

    private readonly LayerReorderService _layerReorderService;

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
        if (pressedListBoxItem?.DataContext is not LayerItem pressedItem) return;

        var selected = LayerListBox.SelectedItems?.OfType<LayerItem>().ToList() ?? [];

        if (!selected.Contains(pressedItem))
            _draggedLayers = [pressedItem.Layer];
        else
            _draggedLayers = [.. selected.OrderBy(LayerListBox.Items.IndexOf).Select(x => x.Layer)];

        _mousePressPos = e.GetPosition(this);
        _itemHeight = (int)pressedListBoxItem.Bounds.Height;
    }

    private void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || LayerManager?.Layers.Count <= 1 || _draggedLayers.Count == 0) return;

        var dx = e.GetPosition(this).X - _mousePressPos.X;
        var dy = e.GetPosition(this).Y - _mousePressPos.Y;

        if (dx * dx + dy * dy < 100) return;

        if (!_dragging)
            StartDragVisual();

        _dragging = true;

        if (_draggedLayers.Count > 3)
        {
            CountBadge.IsVisible = true;
            CountBadgeText.Text = $"{_draggedLayers.Count} layers";
            Avalonia.Controls.Canvas.SetLeft(CountBadge, e.GetPosition(BadgeHost).X + 5);
            Avalonia.Controls.Canvas.SetTop(CountBadge, e.GetPosition(BadgeHost).Y + 5);
        }

        var target = GetTargetIndex(e);

        if (target != _targetIndex)
        {
            _targetIndex = target;
            AnimateItems();
        }

        if (_previewPresenters.Count > 0)
        {
            for (var i = 0; i < _previewPresenters.Count; i++)
            {
                Avalonia.Controls.Canvas.SetTop(_previewPresenters[i], e.GetPosition(FloatingHost).Y + i * _itemHeight);
            }
        }
    }

    private void StartDragVisual()
    {
        _draggedItems.Clear();

        foreach (var layer in _draggedLayers)
        {
            var item = _vm!.LayerItems.FirstOrDefault(x => x.Layer == layer);
            if (item is null) continue;

            var index = LayerListBox.Items.IndexOf(item);
            if (LayerListBox.ContainerFromIndex(index) is ListBoxItem listBoxItem)
            {
                _draggedItems.Add(listBoxItem);
                listBoxItem.Opacity = 0;
            }
        }

        _previewPresenters.Clear();

        var stackCount = _draggedLayers.Count > 3 ? 1 : _draggedLayers.Count;

        for (var i = 0; i < stackCount; i++)
        {
            var item = _vm!.LayerItems.FirstOrDefault(x => x.Layer == _draggedLayers[i]);
            if (item is null) continue;

            var preview = new ContentPresenter
            {
                Content = item,
                ContentTemplate = LayerListBox.ItemTemplate,
                Opacity = 0.85,
                ZIndex = -i // перший елемент зверху
            };

            _previewPresenters.Add(preview);
            FloatingHost.Children.Add(preview);
        }
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        ResetItemsTransform();

        if (_draggedLayers.Count == 0 || LayerManager is null)
        {
            CleanupDrag();
            return;
        }

        if (_targetIndex.HasValue)
            MoveGroupTo(_draggedLayers, _targetIndex.Value);

        CleanupDrag();
    }

    private void MoveGroupTo(List<LayerModel> group, int targetIndex)
    {
        var layers = LayerManager!.Layers;

        // рахуємо target-позицію в термінах "скільки невибраних елементів перед targetIndex"
        var nonSelectedBeforeTarget = 0;
        for (var i = 0; i < targetIndex && i < layers.Count; i++)
        {
            if (!group.Contains(layers[i]))
                nonSelectedBeforeTarget++;
        }

        // видаляємо групу з поточних позицій (запам'ятовуємо порядок - вже збережений в group)
        // і вставляємо назад починаючи з nonSelectedBeforeTarget

        // спочатку прибираємо елементи групи
        var withoutGroup = layers.Where(l => !group.Contains(l)).ToList();

        // вставляємо групу цілим блоком, зберігаючи внутрішній порядок
        var insertAt = Math.Clamp(nonSelectedBeforeTarget, 0, withoutGroup.Count);
        withoutGroup.InsertRange(insertAt, group);

        // тепер переставляємо реальну ObservableCollection відповідно до нового порядку
        ApplyNewOrder(layers, withoutGroup);

        RestoreSelectionFor(group);
    }

    private static void ApplyNewOrder(ObservableCollection<LayerModel> layers, List<LayerModel> newOrder)
    {
        for (var i = 0; i < newOrder.Count; i++)
        {
            var currentIndex = layers.IndexOf(newOrder[i]);
            if (currentIndex != i)
                layers.Move(currentIndex, i);
        }
    }

    private void RestoreSelectionFor(List<LayerModel> group)
    {
        var items = _vm!.LayerItems.Where(li => group.Contains(li.Layer)).ToList();
        LayerListBox.SelectedItems?.Clear();

        foreach (var item in items)
            LayerListBox.SelectedItems?.Add(item);
    }

    private void CleanupDrag()
    {
        FloatingHost.Children.Clear();
        _previewPresenters.Clear();

        foreach (var item in _draggedItems)
            item.Opacity = 1;

        _draggedItems.Clear();
        _draggedLayers.Clear();
        _targetIndex = null;

        if (CountBadge.IsVisible)
        {
            CountBadge.IsVisible = false;
            CountBadgeText.Text = "0";
        }
    }

    private int GetTargetIndex(PointerEventArgs e)
    {
        if (_itemHeight <= 0 || LayerManager is null) return 0;

        var y = e.GetPosition(LayerListBox).Y;

        // рахуємо позицію серед НЕВИБРАНИХ елементів (бо вибрані приховані opacity=0 і не займають місця у сприйнятті)
        var nonSelectedIndex = 0;
        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.Items[i] is not LayerItem item || _draggedLayers.Contains(item.Layer)) continue;

            var itemTop = nonSelectedIndex * _itemHeight;
            if (y < itemTop + _itemHeight / 2.0)
                return i;

            nonSelectedIndex++;
        }

        return LayerListBox.ItemCount;
    }

    private void AnimateItems()
    {
        if (_targetIndex is null) return;

        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.ContainerFromIndex(i) is not ListBoxItem listBoxItem || _draggedItems.Contains(listBoxItem)) continue;

            if (listBoxItem.DataContext is not LayerItem item || _draggedLayers.Contains(item.Layer)) continue;

            double targetY = 0;

            var avgSourceIndex = _draggedLayers
                .Select(l => LayerListBox.Items.IndexOf(_vm!.LayerItems.First(x => x.Layer == l)))
                .Average();

            var stackCount = _draggedLayers.Count > 3 ? 1 : _draggedLayers.Count;

            if (_targetIndex > avgSourceIndex && i > avgSourceIndex && i <= _targetIndex)
                targetY = -stackCount * _itemHeight;
            else if (_targetIndex < avgSourceIndex && i >= _targetIndex && i < avgSourceIndex)
                targetY = stackCount * _itemHeight;

            listBoxItem.RenderTransform = TransformOperations.Parse($"translateY({targetY}px)");
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
