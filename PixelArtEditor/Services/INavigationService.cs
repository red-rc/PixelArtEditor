using System;

namespace PixelArtEditor.Services;

public interface INavigationService
{
    void NavigateTo(object viewModel);
    IObservable<object> WhenCurrentViewChanges();
    object GetViewModel();
}