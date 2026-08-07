using PixelArtEditor.AppServices.Shell;

namespace PixelArtEditor.ViewModels;

public class StartMenuVM() : ReactiveObject
{
    private double _canvasOpacity = 0;
    public double CanvasOpacity
    {
        get => _canvasOpacity;
        set => this.RaiseAndSetIfChanged(ref _canvasOpacity, value);
    }

    private bool _imageVisible = false;
    public bool ImageVisible
    {
        get => _imageVisible;
        set => this.RaiseAndSetIfChanged(ref _imageVisible, value);
    }

    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; } = 
        ReactiveCommand.CreateFromTask(ActionService.ShowCreateWindowAsync);
}
