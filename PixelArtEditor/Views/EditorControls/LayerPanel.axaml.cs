using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.ViewModels;
using System;
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

    private Point _mousePressPos;
    private bool _dragging;
    private ListBoxItem? _draggedItem;

    private int? _sourceIndex;
    private int? _targetIndex;

    private int _itemHeight;

    public LayerPanel()
    {
        _vm = new LayerPanelVM();
        DataContext = _vm;
        InitializeComponent();

        LayerListBox.AddHandler(PointerPressedEvent, OnItemPointerPressed, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerMovedEvent, OnItemPointerMoved, RoutingStrategies.Tunnel);
        LayerListBox.AddHandler(PointerReleasedEvent, OnItemPointerReleased, RoutingStrategies.Tunnel);
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

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || _dragging || LayerManager?.Layers.Count <= 1) return;
        _mousePressPos = e.GetPosition(this);

        _draggedItem = (e.Source as Control)?.FindAncestorOfType<ListBoxItem>();
        _sourceIndex = LayerListBox.Items.IndexOf(_draggedItem?.DataContext);
        _itemHeight = (int)(_draggedItem?.Bounds.Height ?? 0);
    }

    private void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || LayerManager?.Layers.Count <= 1) return;

        var dx = e.GetPosition(this).X - _mousePressPos.X;
        var dy = e.GetPosition(this).Y - _mousePressPos.Y;

        if (dx * dx + dy * dy < 100) return;

        if (!_dragging)
        {
            if (_draggedItem is null) return;

            var preview = new ContentPresenter
            {
                Content = _draggedItem.DataContext,
                ContentTemplate = LayerListBox.ItemTemplate
            };

            FloatingHost.Children.Add(preview);
            _draggedItem.Opacity = 0;
        }

        _dragging = true;

        var target = GetTargetIndex(e);

        if (target != _targetIndex)
        {
            _targetIndex = target;
            AnimateItems();
        }

        if (FloatingHost.Children.FirstOrDefault() is Control child)
        {
            var pos = e.GetPosition(FloatingHost);
            Avalonia.Controls.Canvas.SetTop(child, pos.Y);
        }
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        ResetItemsTransform();

        if (_draggedItem is null) return;

        if (_sourceIndex.HasValue && _targetIndex.HasValue && LayerManager is not null)
        {
            var source = _sourceIndex.Value;
            var target = _targetIndex.Value;

            if (source != target)
                LayerManager.Layers.Move(source, target);
        }

        FloatingHost.Children.Clear();
        _draggedItem.Opacity = 1;

        _sourceIndex = null;
        _targetIndex = null;

        _draggedItem = null;
    }

    private int GetTargetIndex(PointerEventArgs e)
    {
        if (!_sourceIndex.HasValue || _itemHeight <= 0) return _sourceIndex ?? 0;

        var pos = e.GetPosition(LayerListBox);
        var index = (int)(pos.Y / _itemHeight);

        return Math.Clamp(index, 0, LayerListBox.ItemCount - 1);
    }

    private void AnimateItems()
    {
        if (_draggedItem is null) return;
        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.ContainerFromIndex(i) is ListBoxItem item)
            {
                double targetY = 0;

                if (_targetIndex > _sourceIndex && i > _sourceIndex && i <= _targetIndex)
                    targetY = -_draggedItem.Bounds.Height;
                else if (_targetIndex < _sourceIndex  && i >= _targetIndex && i < _sourceIndex)
                    targetY = _draggedItem.Bounds.Height;

                item.RenderTransform = TransformOperations.Parse($"translateY({targetY}px)");
            }
        }
    }

    private void ResetItemsTransform()
    {
        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.ContainerFromIndex(i) is ListBoxItem item)
            {
                var transitions = item.Transitions;
                item.Transitions = null;
                item.RenderTransform = TransformOperations.Identity;
                item.Transitions = transitions;
            }
        }
    }
}
