using Pixeller.AppServices;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;

namespace Pixeller.ViewModels;

public partial class MainWindowVM : ReactiveObject
{
    [Reactive] public partial object CurrentView { get; set; }

    public MenuCommandsVM Menu { get; }

    private readonly ObservableAsPropertyHelper<bool> _canOpenMenu;
    public bool CanOpenMenu => _canOpenMenu.Value;

    public MainWindowVM()
    {
        Services.Navigation.Initialize(this);

        Menu = new MenuCommandsVM();
        CurrentView = new StartMenuVM();

        Observable.Switch(this.WhenAnyValue(view => view.CurrentView)
            .Select(vm => vm is EditorVM editorVM
                ? editorVM.WhenAnyValue(e => e.IsTransforming)
                : Observable.Return(false)))
            .Select(isTransforming => !isTransforming)
            .ToProperty(this, vm => vm.CanOpenMenu, out _canOpenMenu);
    }
}