using PixelArtEditor.Other;
using PixelArtEditor.ViewModels;
using PixelArtEditor.Windows;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices;

public static class ActionService
{
    public static async Task ShowCreateWindowAsync()
    {
        Services.ImageData.Model = await DialogService.ShowDialogAsync<CreateDialogWindow, PixelModel>();
        if (Services.ImageData.Model == null) return;

        if (Services.Navigation.GetViewModel() is not EditorVM)
            Services.Navigation.NavigateTo(new EditorVM());
    }

    public static async Task ShowImportWindowAsync()
    {
        Services.ImageData.Model = await ImageImportService.ImportImageAsync();
        if (Services.ImageData.Model == null) return;

        if (Services.Navigation.GetViewModel() is not EditorVM)
            Services.Navigation.NavigateTo(new EditorVM());
    }

    public static async Task ShowExportWindowAsync()
    {
        await DialogService.ShowDialogAsync<ExportDialogWindow, PixelModel>();
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