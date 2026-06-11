using Avalonia.Controls;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class ExportDialogWindow : Window
{
    public ExportDialogWindow()
    {
        InitializeComponent();
    }

    public ExportDialogWindow(PixelModel model) : this()
    {
        InitializeComponent();
        DataContext = new ExportDialogVM(this, model);
    }
}