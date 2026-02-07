using PixelArtEditor.AppServices;
using ReactiveUI;

namespace PixelArtEditor.ViewModels;

public class MainWindowVM : ReactiveObject
{
    private object _currentView;

    public object CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }
    
    public MenuCommandsVM Menu { get; }

    public MainWindowVM()
    {
        Services.Navigation.Initialize(this);

        Menu = new MenuCommandsVM();
        _currentView = new StartMenuVM();
    }
}
