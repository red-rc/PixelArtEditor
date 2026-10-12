using Avalonia.Controls;
using Pixeller.Models.Canvas;
using Pixeller.ViewModels;

namespace Pixeller.Windows;

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