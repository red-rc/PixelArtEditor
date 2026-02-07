using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PixelArtEditor.AppServices;
using PixelArtEditor.ViewModels;
using System;

namespace PixelArtEditor.Windows;

public partial class MainWindow : Window
{
    private const int EdgeSize = 4;
    private PointerPressedEventArgs? _pressedArgs;
    private PixelPoint _pressedPoint;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowVM();
        Services.WindowState.AttachWindow(this);

        AddHandler(PointerPressedEvent, Resize, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, UpdateCursor, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void Resize(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);

        var left = point.X <= EdgeSize;
        var right = point.X >= Bounds.Width - EdgeSize;
        var top = point.Y <= EdgeSize;
        var bottom = point.Y >= Bounds.Height - EdgeSize;

        WindowEdge? edge = null;
        if (left && top) edge = WindowEdge.NorthWest;
        else if (right && top) edge = WindowEdge.NorthEast;
        else if (left && bottom) edge = WindowEdge.SouthWest;
        else if (right && bottom) edge = WindowEdge.SouthEast;
        else if (left) edge = WindowEdge.West;
        else if (right) edge = WindowEdge.East;
        else if (top) edge = WindowEdge.North;
        else if (bottom) edge = WindowEdge.South;

        if (!edge.HasValue) return;
        BeginResizeDrag(edge.Value, e);
        e.Handled = true;
    }

    private void UpdateCursor(object? sender, PointerEventArgs e)
    {
        if (!CanResize) return;

        var point = e.GetPosition(this);

        var left = point.X <= EdgeSize;
        var right = point.X >= Bounds.Width - EdgeSize;
        var top = point.Y <= EdgeSize;
        var bottom = point.Y >= Bounds.Height - EdgeSize;

        if ((left && top) || (right && bottom))
            Cursor = new Cursor(StandardCursorType.TopLeftCorner);
        else if ((right && top) || (left && bottom))
            Cursor = new Cursor(StandardCursorType.TopRightCorner);

        else if (left || right)
            Cursor = new Cursor(StandardCursorType.SizeWestEast);
        else if (top || bottom)
            Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        else
            Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        Services.WindowState.Current = WindowState.Minimized;
    }
    private void OnMaximizeClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        Services.WindowState.Current = Services.WindowState.Current switch
        {
            WindowState.Maximized => WindowState.Normal,
            WindowState.FullScreen => WindowState.Normal,
            WindowState.Normal => WindowState.Maximized,
            _ => WindowState
        };
    }
    private void OnCloseClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        Close();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _pressedArgs = e;

        var point = e.GetPosition(this);
        _pressedPoint = new PixelPoint((int)point.X, (int)point.Y);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedArgs is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _pressedPoint.X) < 7 && Math.Abs(point.Y - _pressedPoint.Y) < 7) return;

        if (Services.WindowState.Current == WindowState.Maximized || Services.WindowState.Current == WindowState.FullScreen)
        {
            var relativeX = point.X / Bounds.Width;
            var relativeY = point.Y / Bounds.Height;

            Services.WindowState.Current = WindowState.Normal;

            Position = new PixelPoint(
                (int)(point.X - Bounds.Width * relativeX),
                (int)(point.Y - Bounds.Height * relativeY));
        }

        BeginMoveDrag(_pressedArgs);
        _pressedArgs = null;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pressedArgs = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F11) return;

        e.Handled = true;
        if (Services.WindowState.Current == WindowState.FullScreen)
            Services.WindowState.Current = WindowState.Normal;
        else
            Services.WindowState.Current = WindowState.FullScreen;
    }
}