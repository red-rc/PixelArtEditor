using Avalonia.Controls;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class MessageDialogWindow : Window
{
    public MessageDialogWindow(string message, string name)
    {
        InitializeComponent();
        DataContext = new MessageDialogVM(this, message, name);
    }
}