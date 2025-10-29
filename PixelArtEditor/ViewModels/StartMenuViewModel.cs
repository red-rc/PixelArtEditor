using ReactiveUI;
using System.Reactive;
using PixelArtEditor.AppServices;

namespace PixelArtEditor.ViewModels;

public class StartMenuViewModel() : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> CreateCommand { get; } = 
        ReactiveCommand.CreateFromTask(Services.Actions.ShowCreateWindowAsync);

    // ReactiveCommand<Unit, Unit> означає: "Не приймає вхідних даних (Unit) і не повертає нічого (Unit)".
}
