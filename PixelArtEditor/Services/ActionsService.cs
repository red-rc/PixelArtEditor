using System.Threading.Tasks;
using PixelArtEditor.ViewModels;
using PixelArtEditor.Windows;

namespace PixelArtEditor.Services;

public class ActionsService : IActionsService
{
    public async Task ShowCreateWindowAsync(IAppServices services)
    {
        var result = await services.Dialogs.ShowDialogAsync<CreateDialogWindow, CreateParams>();

        if (result != null)
        {
            services.Navigation.NavigateTo(new EditorViewModel(services, result));
        }
    }
    public async Task ShowSettingsWindowAsync(IAppServices services)
    {
        await services.Dialogs.ShowDialogAsync<SettingsDialogWindow>();
    }
}