using Avalonia.Controls;
using Pixeller.Models.Canvas;
using Pixeller.ViewModels;

namespace Pixeller.Windows;

public partial class ExportDialogWindow : Window
{
    public ExportDialogWindow()
    {
        InitializeComponent();
    }

    public ExportDialogWindow(PixelModel model, EditorVM editorVM) : this()
    {
        InitializeComponent();
        DataContext = new ImagePropertiesVM(this, model, editorVM, true);
    }
}