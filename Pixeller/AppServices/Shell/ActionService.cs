using Pixeller.AppServices.ImageProcessing;
using Pixeller.Models.Canvas;
using Pixeller.ViewModels;
using Pixeller.Windows;
using System.Threading.Tasks;

namespace Pixeller.AppServices.Shell;

public static class ActionService
{
    public static async Task ShowCreateWindow()
    {
        var model = await DialogService.ShowDialogAsync<CreateDialogWindow, PixelModel>();
        if (model is null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }

    public static async Task ShowImportDialog()
    {
        var file = await ImageImportService.PickImportFileAsync();
        if (file is null) return;

        if (Services.Navigation.GetViewModel() is ViewBase vb)
        {
            var model = await vb.RunLoadAsync(ImageImportService.GetPixelModelFromFile(file));
            if (model is null) return;

            Services.Navigation.NavigateTo(new EditorVM(model));
        }
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
