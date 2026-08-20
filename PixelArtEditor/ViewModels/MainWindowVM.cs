using PixelArtEditor.AppServices;
using System.Reactive.Linq;

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

    private readonly ObservableAsPropertyHelper<bool> _canOpenMenu;
    public bool CanOpenMenu => _canOpenMenu.Value;

    public MainWindowVM()
    {
        Services.Navigation.Initialize(this);

        Menu = new MenuCommandsVM();
        _currentView = new StartMenuVM();

        this.WhenAnyValue(view => view.CurrentView)
            .Select(vm => vm is EditorVM editorVM
                ? editorVM.WhenAnyValue(e => e.IsTransforming)
                : Observable.Return(false))
            .Switch()
            .Select(isTransforming => !isTransforming)
            .ToProperty(this, vm => vm.CanOpenMenu, out _canOpenMenu);
    }
}
