using Avalonia.Controls;
using Pixeller.Helpers;
using Pixeller.ViewModels;

namespace Pixeller.Windows;

public partial class CreateDialogWindow : Window
{
    public CreateDialogWindow()
    {
        InitializeComponent();
        DataContext = new CreateDialogVM(this);

        ColorPickerStyleFixer.Fix(BackgroundColorPicker);
    }
}