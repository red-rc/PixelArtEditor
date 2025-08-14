namespace PixelArtEditor.Services;

public class AppServices(
    IDialogService dialogs,
    INavigationService navigation,
    IActionsService actions)
    : IAppServices
{
    public IDialogService Dialogs { get; } = dialogs;
    public INavigationService Navigation { get; } = navigation;
    public IActionsService Actions { get; } = actions;
}
