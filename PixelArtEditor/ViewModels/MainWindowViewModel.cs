using PixelArtEditor.AppServices;
using ReactiveUI;

namespace PixelArtEditor.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private object _currentView;

    public object CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }
    
    public MenuCommandsViewModel Menu { get; }

    public MainWindowViewModel()
    {
        Services.Navigation.Initialize(this);

        Menu = new MenuCommandsViewModel();
        _currentView = new StartMenuViewModel();
    }
}
