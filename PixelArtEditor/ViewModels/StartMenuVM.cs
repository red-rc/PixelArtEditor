using PixelArtEditor.AppServices.Shell;
using ReactiveUI.SourceGenerators;

namespace PixelArtEditor.ViewModels;

public partial class StartMenuVM() : ReactiveObject
{
    [Reactive] public partial double DragBgOpacity { get; set; } = 0;
    [Reactive] public partial bool DragImageVisible { get; set; } = false;

    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; } = 
        ReactiveCommand.CreateFromTask(ActionService.ShowCreateWindow);
}
