using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Views;

public partial class MainWindow : Window
{
    private const int EdgeSize = 8;
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        
        AddHandler(PointerPressedEvent, 
            Resize, 
            RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, 
            UpdateCursor, 
            RoutingStrategies.Tunnel);
    }

    private void Resize(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is MenuItem) return;

        var point = e.GetPosition(this);
        
        var left = point.X <= EdgeSize;
        var right = point.X >= Bounds.Width - EdgeSize;
        var top = point.Y <= EdgeSize;
        var bottom = point.Y >= Bounds.Height - EdgeSize;

        WindowEdge? edge = null;
        if (left && top)            edge = WindowEdge.NorthWest;
        else if (right && top)      edge = WindowEdge.NorthEast;
        else if (left && bottom)    edge = WindowEdge.SouthWest;
        else if (right && bottom)   edge = WindowEdge.SouthEast;
        else if (left)              edge = WindowEdge.West;
        else if (right)             edge = WindowEdge.East;
        else if (top)               edge = WindowEdge.North;
        else if (bottom)            edge = WindowEdge.South;

        if (!edge.HasValue) return;
        BeginResizeDrag(edge.Value, e);
        e.Handled = true;
    }

    private void UpdateCursor(object? sender, PointerEventArgs e)
    {
        if (e.Source is MenuItem) return;
        
        var point = e.GetPosition(this);

        var left   = point.X <= EdgeSize;
        var right  = point.X >= Bounds.Width - EdgeSize;
        var top    = point.Y <= EdgeSize;
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
        WindowState = WindowState.Minimized;
    }
    private void OnMaximizeClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        switch (WindowState)
        {
            case WindowState.Maximized:
                WindowState = WindowState.Normal;
                ExtendClientAreaToDecorationsHint = true;
                break;
            case WindowState.Normal:
                WindowState = WindowState.Maximized;
                ExtendClientAreaToDecorationsHint = false;
                break;
            case WindowState.Minimized:
            case WindowState.FullScreen:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    private void OnCloseClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        Close();
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Visual;

        if (source?.GetVisualAncestors().OfType<Menu>().Any() == true || 
            source?.GetVisualAncestors().OfType<MenuItem>().Any() == true)
        {
            return;
        }

        if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}