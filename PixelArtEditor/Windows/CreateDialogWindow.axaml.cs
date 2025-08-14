using Avalonia.Controls;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class CreateDialogWindow : Window
{
    public CreateDialogWindow()
    {
        InitializeComponent();
        DataContext = new CreateDialogViewModel(this);
    }
}