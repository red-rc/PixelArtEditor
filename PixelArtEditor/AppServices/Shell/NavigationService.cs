using System;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.Shell;

public class NavigationService()
{
    private MainWindowVM _mainWindowVM = null!;
    public void Initialize(MainWindowVM mainWindowVM)
    {
        _mainWindowVM = mainWindowVM;
    }
    public void NavigateTo(object VM)
    {
        _mainWindowVM.CurrentView = VM;
    }

    public IObservable<object> WhenCurrentViewChanges() =>
        _mainWindowVM.WhenAnyValue(vm => vm.CurrentView);
    
    public object GetViewModel() => _mainWindowVM.CurrentView;
}
