using Avalonia.Controls;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Windows;

public partial class SettingsDialogWindow : Window
{
    public SettingsDialogWindow()
    {
        InitializeComponent();

        var viewModel = new SettingsDialogVM(this);
        DataContext = viewModel;

        Closing += (_, _) =>
        {
            viewModel.OnClosing();
        };
    }

    public void RestartWindow()
    {
        var position = Position;
        var owner = Owner as Window;

        Hide();

        var currentVm = (SettingsDialogVM)DataContext!;
        var newVm = new SettingsDialogVM(this) { SelectedTabIndex = currentVm.SelectedTabIndex };
        DataContext = newVm;

        Position = position;

        if (owner is not null) Show(owner);
        else Show();
    }
}