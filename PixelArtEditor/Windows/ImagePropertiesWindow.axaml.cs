using Avalonia.Controls;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class ImagePropertiesWindow : Window
{
    public ImagePropertiesWindow()
    {
        InitializeComponent();
        DataContext = new ImagePropertiesVM(this);
    }
}