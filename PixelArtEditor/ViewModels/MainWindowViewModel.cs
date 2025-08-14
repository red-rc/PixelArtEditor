using PixelArtEditor.Services;
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
        var navigation = new NavigationService(this);
        var dialogService = new DialogService();
        var actions = new ActionsService();
    
        var services = new AppServices(dialogService, navigation, actions);
        
        Menu = new MenuCommandsViewModel(services);

        _currentView = new StartMenuViewModel(services);
    }
}
