using System;
using PixelArtEditor.ViewModels;
using ReactiveUI;

namespace PixelArtEditor.Services;

public class NavigationService(MainWindowViewModel mainWindowViewModel) : INavigationService
{
    public void NavigateTo(object viewModel)
    {
        mainWindowViewModel.CurrentView = viewModel;
    }

    public IObservable<object> WhenCurrentViewChanges() =>
        mainWindowViewModel.WhenAnyValue(vm => vm.CurrentView);
    
    public object GetViewModel() => mainWindowViewModel.CurrentView;
}
