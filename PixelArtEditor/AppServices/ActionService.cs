using PixelArtEditor.ViewModels;
using PixelArtEditor.Windows;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices;

public static class ActionService
{
    public static async Task ShowCreateWindowAsync()
    {
        var result = await DialogService.ShowDialogAsync<CreateDialogWindow, CreateParams>();

        if (result != null)
        {
            if (Services.Navigation.GetViewModel() is EditorVM editorVM)
            {
                editorVM.SetInitCreateParams(result);
            }
            else
            {
                Services.Navigation.NavigateTo(new EditorVM(result));
            }
        }
    }

    public static async Task ShowImportWindowAsync()
    {
        var bitmap = await ImageImportService.ImportImageAsync();
        if (bitmap == null) return;

        if (Services.Navigation.GetViewModel() is EditorVM editorVM)
        {
            editorVM.SetInitBitmap(bitmap);
        }
        else
        {
            Services.Navigation.NavigateTo(new EditorVM(bitmap));
        }
    }

    public static async Task ShowExportWindowAsync()
    {
        await DialogService.ShowDialogAsync<ExportDialogWindow, ExportParams>();
    }

    public static async Task ShowSettingsWindowAsync()
    {
        await DialogService.ShowDialogAsync<SettingsDialogWindow>();
    }

    public static async Task ShowImagePropertiesWindowAsync()
    {
        await DialogService.ShowDialogAsync<ImagePropertiesWindow>();
    }
}