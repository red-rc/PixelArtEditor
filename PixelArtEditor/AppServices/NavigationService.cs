using System;
using PixelArtEditor.ViewModels;
using ReactiveUI;

namespace PixelArtEditor.AppServices;

public class NavigationService() : INavigationService
{
    private MainWindowViewModel _mainWindowViewModel = null!;
    public void Initialize(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }
    public void NavigateTo(object viewModel)
    {
        _mainWindowViewModel.CurrentView = viewModel;
    }

    public IObservable<object> WhenCurrentViewChanges() =>
        _mainWindowViewModel.WhenAnyValue(vm => vm.CurrentView);
    
    public object GetViewModel() => _mainWindowViewModel.CurrentView;
}
