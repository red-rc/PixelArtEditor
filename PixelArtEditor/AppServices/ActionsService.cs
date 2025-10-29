using System.Threading.Tasks;
using PixelArtEditor.ViewModels;
using PixelArtEditor.Windows;

namespace PixelArtEditor.AppServices;

public class ActionsService : IActionsService
{
    public async Task ShowCreateWindowAsync()
    {
        var result = await Services.Dialogs.ShowDialogAsync<CreateDialogWindow, CreateParams>();

        if (result != null)
        {
            Services.Navigation.NavigateTo(new EditorViewModel(result));
        }
    }
    public async Task ShowExportWindowAsync()
    {
        var result = await Services.Dialogs.ShowDialogAsync<ExportDialogWindow, ExportParams>();

        if (result != null)
        {
          
        }
    }
    public async Task ShowSettingsWindowAsync()
    {
        await Services.Dialogs.ShowDialogAsync<SettingsDialogWindow>();
    }
}