using PixelArtEditor.ViewModels;
using System;

namespace PixelArtEditor.AppServices;

public interface INavigationService
{
    void Initialize(MainWindowViewModel mainWindowViewModel);
    void NavigateTo(object viewModel);
    IObservable<object> WhenCurrentViewChanges();
    object GetViewModel();
}