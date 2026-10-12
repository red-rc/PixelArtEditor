using Avalonia.Controls;
using Pixeller.Models.Canvas;
using Pixeller.ViewModels;

namespace Pixeller.Windows;

public partial class ImagePropertiesWindow : Window
{
    public ImagePropertiesWindow()
    {
        InitializeComponent();
    }

    public ImagePropertiesWindow(PixelModel model, EditorVM editorVM) : this()
    {
        InitializeComponent();
        DataContext = new ImagePropertiesVM(this, model, editorVM);
    }
}