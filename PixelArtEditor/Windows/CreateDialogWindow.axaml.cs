using Avalonia.Controls;
using PixelArtEditor.Helpers;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class CreateDialogWindow : Window
{
    public CreateDialogWindow()
    {
        InitializeComponent();
        DataContext = new CreateDialogVM(this);

        ColorPickerStyleFixer.Fix(BackgroundColorPicker);
    }
}