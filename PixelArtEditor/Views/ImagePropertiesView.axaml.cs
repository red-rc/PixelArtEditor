using Avalonia.Controls;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Views;

public partial class ImagePropertiesView : UserControl
{
    public ImagePropertiesView()
    {
        InitializeComponent();
        DataContext = new ImagePropertiesViewModel();
    }
}