using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using PixelArtEditor.Windows;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.Shell;

public static class ActionService
{
    public static async Task ShowCreateWindow()
    {
        var model = await DialogService.ShowDialogAsync<CreateDialogWindow, PixelModel>();
        if (model is null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }

    public static async Task ShowImportWindow()
    {
        var model = await ImageImportService.ImportImageAsync();
        if (model is null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }

    public static async Task ShowExportWindow(PixelModel model, EditorVM editorVM)
        => await DialogService.ShowDialogAsync<ExportDialogWindow, PixelModel>(model, editorVM);

    public static async Task ShowSettingsWindow()
        => await DialogService.ShowDialogAsync<SettingsDialogWindow>();

    public static async Task ShowImagePropertiesWindow(PixelModel model, EditorVM editorVM)
        => await DialogService.ShowDialogAsync<ImagePropertiesWindow>(model, editorVM);

    public static async Task ShowIndexedPropertiesWindow(PixelModel model)
        => await DialogService.ShowDialogAsync<IndexedDialogWindow, PixelModel>(model);

    public static async Task ShowError(string message, string name = "Error")
        => await DialogService.ShowDialogAsync<MessageDialogWindow>(message, name);
}