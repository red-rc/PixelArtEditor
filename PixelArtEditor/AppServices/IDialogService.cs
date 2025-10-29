using System.Threading.Tasks;
using Avalonia.Controls;

namespace PixelArtEditor.AppServices;

public interface IDialogService
{
    Task<TResult?> ShowDialogAsync<TWindow, TResult>() where TWindow : Window, new();
    Task ShowDialogAsync<TWindow>() where TWindow : Window, new();
    //Task ShowOpenDialogAsync();
    //Task ShowSettingsDialogAsync();
}
