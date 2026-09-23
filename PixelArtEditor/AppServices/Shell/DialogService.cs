using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.Shell;

public static class DialogService
{
    public static async Task<TResult?> ShowDialogAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
        TWindow, TResult>(params object[] args) where TWindow : Window
    {
        var dialog = (TWindow)Activator.CreateInstance(typeof(TWindow), args)!;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            var parentWindow = lifetime.Windows.FirstOrDefault(w => w.IsActive) ?? lifetime.MainWindow!;
            return await dialog.ShowDialog<TResult?>(parentWindow);
        }

        return default;
    }

    public static async Task ShowDialogAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
        TWindow>(params object[] args) where TWindow : Window
    {
        var dialog = (TWindow)Activator.CreateInstance(typeof(TWindow), args)!;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            await dialog.ShowDialog(lifetime.MainWindow!);
    }
}