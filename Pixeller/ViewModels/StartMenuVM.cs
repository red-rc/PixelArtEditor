using Pixeller.AppServices;
using Pixeller.AppServices.Shell;
using ReactiveUI.SourceGenerators;

namespace Pixeller.ViewModels;

public partial class StartMenuVM : ViewBase
{
    [Reactive] public partial string AppVersion { get; set; } = "v" + ResourceManager.AppVersion;

    [Reactive] public partial double DragBgOpacity { get; set; } = 0;
    [Reactive] public partial bool DragImageVisible { get; set; } = false;

    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; } =
        ReactiveCommand.CreateFromTask(ActionService.ShowCreateWindow);
}