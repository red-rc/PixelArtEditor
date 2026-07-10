using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.EditorUI;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Dock;
using PixelArtEditor.UI;
using PixelArtEditor.ViewModels;
using System;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;

namespace PixelArtEditor.Views;

public partial class EditorView : UserControl
{
    private DockState? _dockState;

    private Point _mousePressPos;
    private bool _pressedOnPanel;
    private bool _dragging;

    private readonly LayoutManager _layoutManager;
    private readonly TooltipManager _tooltipManager;

    private EditorVM? ViewModel => DataContext as EditorVM;

    public EditorView()
    {
        InitializeComponent();

        _layoutManager = new LayoutManager(MainLayout, RectHost, CanvasPanel);
        _tooltipManager = new TooltipManager(Tooltip, TooltipText, RectHost);

        AddHandler(KeyDownEvent, OnLayerHotkeys, RoutingStrategies.Tunnel);

        this.DataContextChanged += OnDataContextChanged;

        AttachedToVisualTree += (s, e) =>
        {
            Services.ModelData.ModelChanged += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ViewModel is null || CanvasPanel.Bounds is not { Width: > 0, Height: > 0 }) return;
                    ViewModel.AdjustCanvas(CanvasPanel.Bounds.Width, CanvasPanel.Bounds.Height);
                });
            };

            Services.Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(SettingsManager.Layout)) return;
                _layoutManager.LoadLayout();
            };

            _layoutManager.InitializeRects();
            _layoutManager.LoadLayout();

            MainLayout.LayoutUpdated += OnMainLayoutLayoutUpdated;
        };
    }

    private async void OnLayerHotkeys(object? sender, KeyEventArgs e)
    {
        var vm = LayerPanelControl.ViewModel;

        switch (e.KeyModifiers, e.Key)
        {
            case (KeyModifiers.Control, Key.N):
                vm.AddCommand.Execute().Subscribe();
                break;

            case (KeyModifiers.Control, Key.D):
                vm.DuplicateCommand.Execute().Subscribe();
                break;

            case (KeyModifiers.Control, Key.Up):
                LayerPanelControl.MoveSelectedStep(-1);
                break;

            case (KeyModifiers.Control, Key.Down):
                LayerPanelControl.MoveSelectedStep(1);
                break;

            case (_, Key.Delete):
                vm.RemoveCommand.Execute().Subscribe();
                break;
            default:
                return;
        }

        e.Handled = true;
        Dispatcher.UIThread.Post(() => Root.Focus());
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is EditorVM vm)
        {
            LayerPanelControl.LayerManager = null;

            vm.SetCanvas(CanvasControl);
            LayerPanelControl.LayerManager = CanvasControl.LayerManager;

            vm.AdjustCanvas(CanvasPanel.Bounds.Width, CanvasPanel.Bounds.Height);

            Dispatcher.UIThread.Post(() => Root.Focus());
        }
    }

    private void CanvasPanel_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (ViewModel == null || e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;
        ViewModel.AdjustCanvas(e.NewSize.Width, e.NewSize.Height);

        Dispatcher.UIThread.Post(() => Root.Focus(), DispatcherPriority.Input);
    }

    private void CanvasPanel_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel == null || e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed) return;

        var rawScale = ViewModel.Scale + e.Delta.Y * ViewModel.Scale / 10.0;

        var newScale = Math.Clamp(rawScale, ViewModel.MinScale, ViewModel.MaxScale);

        if (Math.Abs(newScale - ViewModel.Scale) < 1e-9) return;

        var mousePos = e.GetPosition(CanvasPanel);
        var center = new Point(CanvasPanel.Bounds.Width / 2, CanvasPanel.Bounds.Height / 2);

        var screenVec = new Vector2(
            (float)mousePos.X - (float)center.X - ViewModel.Offset.X,
            (float)mousePos.Y - (float)center.Y - ViewModel.Offset.Y);

        var newScreenVec = screenVec / (float)ViewModel.Scale * (float)newScale;

        var correctedOffset = new Vector2(
            ViewModel.Offset.X + (screenVec.X - newScreenVec.X),
            ViewModel.Offset.Y + (screenVec.Y - newScreenVec.Y));

        ViewModel.Scale = newScale;
        ViewModel.Offset = correctedOffset;
    }

    private void CanvasPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel == null
            || !e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed
            || ViewModel.IsPositionSet
            || !ViewModel.IsHandEnabled) return;
        ViewModel.StartDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel == null
            || !e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed
            || !ViewModel.IsPositionSet
            || !ViewModel.IsHandEnabled) return;
        ViewModel.UpdateDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ViewModel == null) return;
        ViewModel.IsPositionSet = false;
    }

    private void OnMainLayoutLayoutUpdated(object? sender, EventArgs e) => _layoutManager.UpdateRectPositions();

    private void Root_PointerMoved(object? sender, PointerEventArgs e)
    {
        var buttons = LayerPanelControl.GetVisualDescendants().OfType<Button>()
            .Concat(ToolbarPanel.StackPanel.Children.OfType<Button>());

        _tooltipManager.OnPointerMoved(e, buttons);
    } 

    private async void Panel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || _dragging) return;

        var source = e.Source as Control;
        _pressedOnPanel = source is not (Button or InstantToggleButton or TextBox or ComboBox or ListBox or ListBoxItem);
        _mousePressPos = e.GetPosition(this);
    }

    private void Panel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control draggedPanel || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !_pressedOnPanel) return;

        var mousePos = e.GetPosition(this);
        var dx = mousePos.X - _mousePressPos.X;
        var dy = mousePos.Y - _mousePressPos.Y;

        if (dx * dx + dy * dy < 100) return;

        if (!_dragging)
        {
            _dockState ??= DockHelper.GetDockState(draggedPanel);
            DockManager.Undock(draggedPanel, FloatingHost);

            var rectsToHighlight = _dockState.Orientation == DockOrientation.Vertical ? _layoutManager.VerticalRects : _layoutManager.HorizontalRects;

            rectsToHighlight.ForEach(rect => rect.Fill = rect.GetValue(LayoutManager.DockInfoProperty)?.Orientation == _dockState.Orientation ?
                new SolidColorBrush(Application.Current?.Resources["PrimaryPressedColor"] as Color? ?? Colors.Blue) : rect.Fill);

            e.Pointer.Capture(draggedPanel);
        }

        _dragging = true;

        var pos = e.GetPosition(FloatingHost);
        Avalonia.Controls.Canvas.SetLeft(draggedPanel, pos.X);
        Avalonia.Controls.Canvas.SetTop(draggedPanel, pos.Y);
    }

    private void Panel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pressedOnPanel = false;
        _dragging = false;

        if (sender is not Control draggedPanel || _dockState is null) return;
        e.Pointer.Capture(null);

        var docked = false;
        var rectsToCheck = _dockState.Orientation == DockOrientation.Vertical ? _layoutManager.VerticalRects : _layoutManager.HorizontalRects;

        foreach (var rect in rectsToCheck)
        {
            var dockInfo = rect.GetValue(LayoutManager.DockInfoProperty);

            if (dockInfo is null || _dockState is null) continue;

            if (rect.Bounds.Inflate(30).Contains(e.GetPosition(RectHost)))
            {
                DockManager.Redock(draggedPanel, _dockState);
                DockManager.ReorderElements(_dockState.OriginalParent, draggedPanel, dockInfo.Index, dockInfo.Orientation, _dockState, _layoutManager.ApplyGridDefinitions);

                Services.Settings.Layout = [.. MainLayout.Children
                    .OfType<Control>()
                    .Select(c => new PanelLayout { Name = c.Name, Row = Grid.GetRow(c), Col = Grid.GetColumn(c) })];

                docked = true;
            }

            var boundaryCount = dockInfo.Orientation == DockOrientation.Vertical
                ? MainLayout.ColumnDefinitions.Count
                : MainLayout.RowDefinitions.Count;

            rect.Fill = (dockInfo.Index == 0 || dockInfo.Index == boundaryCount) ? new SolidColorBrush(Colors.Transparent) : 
                new SolidColorBrush(Application.Current?.Resources["BackgroundPressedColor"] as Color? ?? Colors.DarkGray);
        }

        if (!docked)
        {
            DockManager.Redock(draggedPanel, _dockState!);
            Grid.SetRow(draggedPanel, _dockState!.Row);
            Grid.SetColumn(draggedPanel, _dockState!.Column);
        }

        _layoutManager.UpdateRectPositions();
        _dockState = null;

        _tooltipManager.Hide();
    }
}