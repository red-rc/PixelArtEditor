using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.EditorUI;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Dock;
using PixelArtEditor.ViewModels;
using System;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.Views;

public partial class EditorView : UserControl
{
    private DockState? _dockState;

    private bool _dragging;
    private bool _pointerDown;

    private readonly LayoutManager _layoutManager;
    private readonly DockManager _dockManager;
    private readonly TooltipManager _tooltipManager;

    private EditorVM? ViewModel => DataContext as EditorVM;

    public EditorView()
    {
        InitializeComponent();

        _layoutManager = new LayoutManager(MainLayout, RectHost, CanvasPanel);
        _dockManager = new DockManager(FloatingHost, _layoutManager.ApplyGridDefinitions);
        _tooltipManager = new TooltipManager(Tooltip, TooltipText, RectHost);

        AttachedToVisualTree += (s, e) =>
        {
            if (DataContext is EditorVM vm)
            {
                vm.SetCanvas(CanvasControl);
            }

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

    private void CanvasPanel_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (ViewModel == null || e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;
        ViewModel.AdjustCanvas(e.NewSize.Width, e.NewSize.Height);
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

    private void Root_PointerMoved(object? sender, PointerEventArgs e) => 
        _tooltipManager.OnPointerMoved(e, ToolbarPanel.StackPanel.Children.OfType<Control>());

    private async void Panel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control dragged || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        
        _pointerDown = true;
        await Task.Delay(500);
    
        if (!_pointerDown) return;
    
        _dockState ??= DockHelper.GetDockState(dragged);
        _dockManager.Undock(dragged);

        _dragging = true;
    
        var rectsToHighlight = _dockState.Orientation == DockOrientation.Vertical ? _layoutManager.VerticalRects : _layoutManager.HorizontalRects;

        rectsToHighlight.ForEach(rect => rect.Fill = rect.GetValue(LayoutManager.DockInfoProperty)?.Orientation == _dockState.Orientation ?
            new SolidColorBrush(Application.Current?.Resources["PrimaryPressedColor"] as Color? ?? Colors.Blue) : rect.Fill);
    
        e.Pointer.Capture(dragged);
    }

    private void Panel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || sender is not Control dragged) return;

        var pos = e.GetPosition(FloatingHost);
        Canvas.SetLeft(dragged, pos.X);
        Canvas.SetTop(dragged, pos.Y);
    }

    private void Panel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pointerDown = false;
    
        if (sender is not Control dragged || _dockState is null)
        {
            _dragging = false;
            return;
        }

        _dragging = false;
        e.Pointer.Capture(null);

        var docked = false;
        var rectsToCheck = _dockState.Orientation == DockOrientation.Vertical ? _layoutManager.VerticalRects : _layoutManager.HorizontalRects;

        foreach (var rect in rectsToCheck)
        {
            var dockInfo = rect.GetValue(LayoutManager.DockInfoProperty);

            if (dockInfo is null || _dockState is null) continue;

            if (rect.Bounds.Inflate(10).Contains(e.GetPosition(RectHost)))
            {
                DockManager.Redock(dragged, _dockState);
                _dockManager.ReorderElements(_dockState.OriginalParent, dragged, dockInfo.Index, dockInfo.Orientation, _dockState);

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
            DockManager.Redock(dragged, _dockState!);
            Grid.SetRow(dragged, _dockState!.Row);
            Grid.SetColumn(dragged, _dockState!.Column);
        }

        _layoutManager.UpdateRectPositions();
        _dockState = null;

        _tooltipManager.Hide();
    }
}