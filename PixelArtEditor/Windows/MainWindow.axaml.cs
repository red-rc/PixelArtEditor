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
    private PixelPoint _pressedPoint;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowVM();
        Services.WindowState.AttachWindow(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var point = e.GetPosition(this);
        _pressedPoint = new PixelPoint((int)point.X, (int)point.Y);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

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
    }
}