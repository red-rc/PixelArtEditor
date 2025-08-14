namespace PixelArtEditor.Services;

public interface IAppServices
{
    IDialogService Dialogs { get; }
    INavigationService Navigation { get; }
    IActionsService Actions { get; }
}