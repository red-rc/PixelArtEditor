using Avalonia.Controls;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Shell;
using System;
using System.Diagnostics;
using System.Numerics;

namespace PixelArtEditor.ViewModels;

public class MenuCommandsVM : ReactiveObject
{
    private static readonly ISettingsManager _settings = Services.Settings;
    public ReactiveCommand<RxVoid, RxVoid> CreateCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> OpenCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ImportCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveAsCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ExportCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> LastAutosaveCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ExitCommand { get; }
  
    public ReactiveCommand<RxVoid, RxVoid> UndoCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> RedoCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ImagePropertiesCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SettingsCommand { get; }
    
    public ReactiveCommand<RxVoid, RxVoid> ZoomInCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ZoomOutCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ResetZoomCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ResetLayout { get; }
    public ReactiveCommand<RxVoid, RxVoid> StandartCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> FullScreenCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> LightThemeCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> DarkThemeCommand { get; }
    
    public ReactiveCommand<RxVoid, RxVoid> CheckForUpdatesCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ReportCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ContactUsCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> AboutCommand { get; }

    public MenuCommandsVM()
    {
        var isDocumentOpen = Services
            .Navigation
            .WhenCurrentViewChanges()
            .Select(view => view is EditorVM)
            .DistinctUntilChanged();

        var isFullScreen = Services.WindowState
            .WhenAnyValue(x => x.Current)
            .Select(s => s == WindowState.FullScreen)
            .DistinctUntilChanged();


        CreateCommand = ReactiveCommand.CreateFromTask(ActionService.ShowCreateWindowAsync);
        OpenCommand = ReactiveCommand.Create(OnOpen);
        ImportCommand = ReactiveCommand.Create(OnImport);
        SaveCommand = ReactiveCommand.Create(OnSave, isDocumentOpen); 
        SaveAsCommand = ReactiveCommand.Create(OnSaveAs, isDocumentOpen);
        ExportCommand = ReactiveCommand.Create(OnExport, isDocumentOpen);
        LastAutosaveCommand = ReactiveCommand.Create(OnLastAutosave); //TODO: Add condition if last save exists
        ExitCommand = ReactiveCommand.Create(() => Services.Navigation.NavigateTo(new StartMenuVM()), isDocumentOpen);
    
        UndoCommand = ReactiveCommand.Create(OnUndo, isDocumentOpen);
        RedoCommand = ReactiveCommand.Create(OnRedo, isDocumentOpen);
        ImagePropertiesCommand = ReactiveCommand.Create(OnImageProperties, isDocumentOpen);
        SettingsCommand = ReactiveCommand.CreateFromTask(ActionService.ShowSettingsWindowAsync);
        
        ZoomInCommand = ReactiveCommand.Create(OnZoomIn, isDocumentOpen);
        ZoomOutCommand = ReactiveCommand.Create(OnZoomOut, isDocumentOpen);
        ResetZoomCommand = ReactiveCommand.Create(OnResetZoom, isDocumentOpen);
        ResetLayout = ReactiveCommand.Create(OnResetLayout, isDocumentOpen);
        StandartCommand = ReactiveCommand.Create(OnStandart, isFullScreen);
        FullScreenCommand = ReactiveCommand.Create(OnFullScreen, isFullScreen.Select(x => !x));
        LightThemeCommand = ReactiveCommand.Create(OnLightTheme);
        DarkThemeCommand = ReactiveCommand.Create(OnDarkTheme);
        
        CheckForUpdatesCommand = ReactiveCommand.Create(OnCheckForUpdates);
        ReportCommand = ReactiveCommand.Create(() => OpenUrl("https://github.com/red-rc/PixelArtEditor/issues"));
        ContactUsCommand = ReactiveCommand.Create(() => OpenUrl("https://mail.google.com/mail/u/0/?to=redthar7@gmail.com&fs=1&tf=cm"));
        AboutCommand = ReactiveCommand.Create(() => OpenUrl("https://github.com/red-rc/PixelArtEditor"));
    }
    
    private void OnOpen()
    {
    }
    
    private async void OnImport() => await ActionService.ShowImportWindowAsync();
    
    private void OnSave()
    {
    }

    private void OnSaveAs()
    {
    }

    private async void OnExport()
    {
        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        await ActionService.ShowExportWindowAsync(editorVM.GetPreparedModel());
    }

    private void OnLastAutosave()
    {
    }

    private void OnUndo()
    {
    }

    private void OnRedo()
    {
    }

    private async void OnImageProperties()
    {
        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        await ActionService.ShowImagePropertiesWindowAsync(editorVM.GetPreparedModel());
    }

    private static void OnZoomIn()
    {
        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        editorVM.ZoomBy(1.2f);
    }

    private static void OnZoomOut()
    {
        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        editorVM.ZoomBy(0.8f);
    }

    private static void OnResetZoom()
    {
        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        editorVM.Scale = editorVM.BaseScale;
        editorVM.Offset = Vector2.Zero;
    }

    private static void OnResetLayout()
    {
        Services.Settings.Layout = ResourceManager.DefaultLayout;
    }

    private static void OnStandart() => Services.WindowState.Current = Services.WindowState.PreviousWindowState;

    private static void OnFullScreen() => Services.WindowState.Current = WindowState.FullScreen;

    private static void OnLightTheme()
    {
        if (_settings.Theme is not "Light") _settings.Theme = "Light";
        _settings.Save();
    }

    private static void OnDarkTheme()
    {
        if (_settings.Theme is not "Dark") _settings.Theme = "Dark";
        _settings.Save();
    }

    private void OnCheckForUpdates()
    {
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception) {  }
    }
}