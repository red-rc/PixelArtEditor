using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Dock;
using PixelArtEditor.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI;

public class PanelDragController(Grid mainLayout, Avalonia.Controls.Canvas floatingHost, Avalonia.Controls.Canvas rectHost, LayoutManager layoutManager)
{
    private DockState? _dockState;
    private Point _mousePressPos;
    private bool _pressedOnPanel;
    private bool _dragging;
    private double _dragOffsetX, _dragOffsetY;

    public bool IsDragging => _dragging;

    public void OnPointerPressed(Control root, PointerPressedEventArgs e) 
    {
        if (_dragging) return;

        var source = e.Source as Control;
        var isExcludedControl = source is Button or InstantToggleButton or TextBox or ComboBox or ListBox or ListBoxItem;
        var isScrollViewerBorder = source is Border && source.GetVisualAncestors().Any(x => x is ScrollViewer);

        _pressedOnPanel = !isExcludedControl && !isScrollViewerBorder;

        _mousePressPos = e.GetPosition(root);
    }

    public void OnPointerMoved(Control draggedPanel, Control root, PointerEventArgs e) 
    {
        if (!_pressedOnPanel) return;

        if (!_dragging)
        {
            var mousePos = e.GetPosition(root);
            var dx = mousePos.X - _mousePressPos.X;
            var dy = mousePos.Y - _mousePressPos.Y;

            if (dx * dx + dy * dy < 100) return;

            _dragOffsetX = e.GetPosition(draggedPanel).X;
            _dragOffsetY = e.GetPosition(draggedPanel).Y;

            _dockState ??= DockHelper.GetDockState(draggedPanel);
            DockManager.Undock(draggedPanel, floatingHost);

            var rectsToHighlight = _dockState.Orientation == DockOrientation.Vertical
                ? layoutManager.VerticalRects : layoutManager.HorizontalRects;

            rectsToHighlight.ForEach(rect => rect.Fill = rect.GetValue(LayoutManager.DockInfoProperty)?.Orientation == _dockState.Orientation
                ? new SolidColorBrush(Application.Current?.Resources["PrimaryPressColor"] as Color? ?? Colors.Blue) : rect.Fill);

            e.Pointer.Capture(draggedPanel);
        }

        _dragging = true;

        var pos = e.GetPosition(floatingHost);
        Avalonia.Controls.Canvas.SetLeft(draggedPanel, pos.X - _dragOffsetX);
        Avalonia.Controls.Canvas.SetTop(draggedPanel, pos.Y - _dragOffsetY);
    }
    public void OnPointerReleased(Control? draggedPanel, PointerReleasedEventArgs e)
    {
        _pressedOnPanel = false;
        _dragging = false;

        if (draggedPanel is null || _dockState is null) return;
        e.Pointer.Capture(null);

        var topLeft = draggedPanel.TranslatePoint(new Point(0, 0), rectHost) ?? default;
        var panelBounds = new Rect(topLeft, draggedPanel.Bounds.Size);

        Rectangle? bestRect = null;
        var bestDistance = double.MaxValue;

        var rectsToCheck = _dockState.Orientation == DockOrientation.Vertical
            ? layoutManager.VerticalRects : layoutManager.HorizontalRects;

        foreach (var rect in rectsToCheck)
        {
            var dockInfo = rect.GetValue(LayoutManager.DockInfoProperty);
            if (dockInfo is null) continue;

            var distance = DistanceBetween(panelBounds, rect.Bounds, _dockState.Orientation);
            if (distance <= 30 && distance < bestDistance)
            {
                bestDistance = distance;
                bestRect = rect;
            }

            var boundaryCount = dockInfo.Orientation == DockOrientation.Vertical
                ? mainLayout.ColumnDefinitions.Count
                : mainLayout.RowDefinitions.Count;

            rect.Fill = (dockInfo.Index == 0 || dockInfo.Index == boundaryCount) ? new SolidColorBrush(Colors.Transparent) :
                new SolidColorBrush(Application.Current?.Resources["BackgroundPressColor"] as Color? ?? Colors.DarkGray);
        }

        DockManager.Redock(draggedPanel, _dockState);

        if (bestRect is not null)
        {
            var dockInfo = bestRect.GetValue(LayoutManager.DockInfoProperty)!;

            DockManager.ReorderElements(
                _dockState.OriginalParent,
                draggedPanel, dockInfo.Index,
                dockInfo.Orientation,
                _dockState, layoutManager.ApplyGridDefinitions);

            Services.Settings.Layout = [.. mainLayout.Children
                .OfType<Control>()
                .Select(c => new PanelLayout { Name = c.Name, Row = Grid.GetRow(c), Col = Grid.GetColumn(c) })];
        }
        else
        {
            Grid.SetRow(draggedPanel, _dockState.Row);
            Grid.SetColumn(draggedPanel, _dockState.Column);
        }

        layoutManager.UpdateRectPositions();
        _dockState = null;
    }

    private static double DistanceBetween(Rect panel, Rect rect, DockOrientation orientation)
    {
        if (orientation == DockOrientation.Vertical)
        {
            var panelEdge = rect.Center.X < panel.Center.X ? panel.Left : panel.Right;
            return Math.Abs(panelEdge - rect.Center.X);
        }
        else
        {
            var panelEdge = rect.Center.Y < panel.Center.Y ? panel.Top : panel.Bottom;
            return Math.Abs(panelEdge - rect.Center.Y);
        }
    }
}
