using PixelArtEditor.AppServices.Shell;

namespace PixelArtEditor.ViewModels;

public class StartMenuVM() : ReactiveObject
{
    private double _dragBgOpacity = 0;
    public double DragBgOpacity
    {
        get => _dragBgOpacity;
        set => this.RaiseAndSetIfChanged(ref _dragBgOpacity, value);
    }

    private bool _dragImageVisible = false;
    public bool DragImageVisible
    {
        get => _dragImageVisible;
        set => this.RaiseAndSetIfChanged(ref _dragImageVisible, value);
    }

    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; } = 
        ReactiveCommand.CreateFromTask(ActionService.ShowCreateWindowAsync);
}
