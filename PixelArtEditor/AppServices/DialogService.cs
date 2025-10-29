using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PixelArtEditor.AppServices;

public class DialogService : IDialogService
{
    public async Task<TResult?> ShowDialogAsync<TWindow, TResult>() where TWindow : Window, new()
    {
        var dialog = new TWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return await dialog.ShowDialog<TResult?>(lifetime.MainWindow!);
        }

        return default;
    }

    public async Task ShowDialogAsync<TWindow>() where TWindow : Window, new()
    {
        var dialog = new TWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            await dialog.ShowDialog(lifetime.MainWindow!);
        }
    }

    //public async Task ShowOpenDialogAsync()
    //{
    //    var dialog = new OpenDialogWindow();
//
    //    if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
    //    {
    //        var result = await dialog.ShowDialog(lifetime.MainWindow!);
    //        return result;
    //    }
//
    //    return null;
    //}
//
    //public async Task ShowSettingsDialogAsync()
    //{
    //    var dialog = new SettingsDialogWindow();
//
    //    if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
    //    {
    //        await dialog.ShowDialog(lifetime.MainWindow!);
    //    }
    //}
}