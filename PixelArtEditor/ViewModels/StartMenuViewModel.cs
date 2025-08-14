using ReactiveUI;
using System.Reactive;
using PixelArtEditor.Services;

namespace PixelArtEditor.ViewModels;

public class StartMenuViewModel(IAppServices services) : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> CreateCommand { get; } = ReactiveCommand.CreateFromTask(() => services.Actions.ShowCreateWindowAsync(services));

    // ReactiveCommand<Unit, Unit> означає: "Не приймає вхідних даних (Unit) і не повертає нічого (Unit)".
}
