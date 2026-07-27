using PixelArtEditor.AppServices.Shell;

namespace PixelArtEditor.ViewModels;

public class StartMenuVM() : ReactiveObject
{
    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; } = 
        ReactiveCommand.CreateFromTask(ActionService.ShowCreateWindowAsync);

    // ReactiveCommand<RxVoid, RxVoid> означає: "Не приймає вхідних даних (RxVoid) і не повертає нічого (RxVoid)".
}
