using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using PixelArtEditor.Windows;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.Shell;

public static class ActionService
{
    public static async Task ShowCreateWindowAsync()
    {
        var model = await DialogService.ShowDialogAsync<CreateDialogWindow, PixelModel>();
        if (model == null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }

    public static async Task ShowImportWindowAsync()
    {
        var model = await ImageImportService.ImportImageAsync();
        if (model == null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }

    public static async Task ShowExportWindowAsync(PixelModel model)
        => await DialogService.ShowDialogAsync<ExportDialogWindow, PixelModel>(model);

    public static async Task ShowSettingsWindowAsync()
        => await DialogService.ShowDialogAsync<SettingsDialogWindow>();

    public static async Task ShowImagePropertiesWindowAsync(PixelModel model)
        => await DialogService.ShowDialogAsync<ImagePropertiesWindow>(model);
}