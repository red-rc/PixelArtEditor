using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class MessageDialogWindow : Window
{
    public MessageDialogWindow(string message, string name)
    {
        InitializeComponent();
        DataContext = new MessageDialogVM(this, message, name);
    }

    public MessageDialogWindow() => InitializeComponent();

    private async void CopyClick(object? sender, RoutedEventArgs e)
    {
        if (GetTopLevel(this)?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(MsgText.Text);
    }
}
