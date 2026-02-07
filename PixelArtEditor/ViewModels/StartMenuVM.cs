using ReactiveUI;
using System.Reactive;
using PixelArtEditor.AppServices;

namespace PixelArtEditor.ViewModels;

public class StartMenuVM() : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> CreateCommand { get; } = 
        ReactiveCommand.CreateFromTask(ActionService.ShowCreateWindowAsync);

    // ReactiveCommand<Unit, Unit> означає: "Не приймає вхідних даних (Unit) і не повертає нічого (Unit)".
}
