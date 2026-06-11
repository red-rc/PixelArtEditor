using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.Shell;

public static class DialogService
{
    public static async Task<TResult?> ShowDialogAsync<TWindow, TResult>(params object[] args) where TWindow : Window
    {
        var dialog = (TWindow)Activator.CreateInstance(typeof(TWindow), args)!;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return await dialog.ShowDialog<TResult?>(lifetime.MainWindow!);
        }

        return default;
    }

    public static async Task ShowDialogAsync<TWindow>(params object[] args) where TWindow : Window
    {
        var dialog = (TWindow)Activator.CreateInstance(typeof(TWindow), args)!;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            await dialog.ShowDialog(lifetime.MainWindow!);
        }
    }
}