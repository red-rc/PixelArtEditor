using Avalonia.Controls;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class IndexedDialogWindow : Window
{
    public IndexedDialogWindow()
    {
        InitializeComponent();
    }

    public IndexedDialogWindow(PixelModel model) : this()
    {
        InitializeComponent();
        DataContext = new IndexedPropertiesVM(this, model);
    }
}