using Avalonia.Controls;

namespace PixelArtEditor.ViewModels;

public class MessageDialogVM(Window dialog, string message, string name)
{
    public string Name { get; set; } = name;
    public string Message { get; set; } = message;
    public ReactiveCommand<RxVoid, RxVoid> OKCommand { get; } = ReactiveCommand.Create(dialog.Close);
}
