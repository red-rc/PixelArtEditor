using Avalonia.Controls;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class ImagePropertiesWindow : Window
{
    public ImagePropertiesWindow()
    {
        InitializeComponent();
    }

    public ImagePropertiesWindow(PixelModel model) : this()
    {
        InitializeComponent();
        DataContext = new ImagePropertiesVM(this, model);
    }
}