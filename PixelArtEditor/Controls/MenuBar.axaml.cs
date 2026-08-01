using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace PixelArtEditor.Controls;

public partial class MenuBar : UserControl
{
    public MenuBar()
    {
        InitializeComponent();
    }

    private void OnMenuFlyoutOpened(object? sender, EventArgs e)
    {
        if (sender is Flyout { Target: Button btn })
            btn.Classes.Add("MenuOpen");
    }

    private void OnMenuFlyoutClosed(object? sender, EventArgs e)
    {
        if (sender is Flyout { Target: Button btn })
            btn.Classes.Remove("MenuOpen");
    }

    private void CloseFileFlyout(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => MenuItem.Flyout?.Hide(), DispatcherPriority.Background);
    }
}