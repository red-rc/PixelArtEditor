using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PixelArtEditor.AppServices;

namespace PixelArtEditor.Windows;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();
        Services.WindowState.AttachWindow(this);

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
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