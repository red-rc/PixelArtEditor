using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PixelArtEditor.AppServices;

public static class DialogService
{
    public static async Task<TResult?> ShowDialogAsync<TWindow, TResult>() where TWindow : Window, new()
    {
        var dialog = new TWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return await dialog.ShowDialog<TResult?>(lifetime.MainWindow!);
        }

        return default;
    }

    public static async Task ShowDialogAsync<TWindow>() where TWindow : Window, new()
    {
        var dialog = new TWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            await dialog.ShowDialog(lifetime.MainWindow!);
        }
    }
}