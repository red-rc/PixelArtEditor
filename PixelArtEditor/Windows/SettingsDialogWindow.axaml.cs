using Avalonia.Controls;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class SettingsDialogWindow : Window
{
    public SettingsDialogWindow()
    {
        InitializeComponent();

        var viewModel = new SettingsDialogViewModel(this);
        DataContext = viewModel;

        Closing += (_, _) =>
        {
            viewModel.OnClosing();
        };
    }
}