using Avalonia.Controls;
using PixelArtEditor.AppServices;
using ReactiveUI;
using System;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;

namespace PixelArtEditor.ViewModels;

public class MenuCommandsVM : ReactiveObject
{
    private static readonly ISettingsService _settings = Services.Settings;
    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> LastAutosaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
  
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }
    public ReactiveCommand<Unit, Unit> ImagePropertiesCommand { get; }
    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetZoomCommand { get; }
    public ReactiveCommand<Unit, Unit> StandartCommand { get; }
    public ReactiveCommand<Unit, Unit> FullScreenCommand { get; }
    public ReactiveCommand<Unit, Unit> LightThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> DarkThemeCommand { get; }
    
    public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> ContactUsCommand { get; }
    public ReactiveCommand<Unit, Unit> AboutCommand { get; }

    /*
        Create New
        Open
        Import
        Save
        Save As
        Export
        Last Autosave
        Exit
        
        Undo
        Redo
        Image Properties
        Settings
        
        Zoom In
        Zoom Out
        Reset Zoom
        Standart
        Fullscreen
        White Theme
        Dark Theme

        Check For Updates
        Contact Us
        About
     */

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
        StandartCommand = ReactiveCommand.Create(OnStandart, isFullScreen);
        FullScreenCommand = ReactiveCommand.Create(OnFullScreen, isFullScreen.Select(x => !x));
        LightThemeCommand = ReactiveCommand.Create(OnLightTheme);
        DarkThemeCommand = ReactiveCommand.Create(OnDarkTheme);
        
        CheckForUpdatesCommand = ReactiveCommand.Create(OnCheckForUpdates);
        ContactUsCommand = ReactiveCommand.Create(OnContactUs);
        AboutCommand = ReactiveCommand.Create(OnAbout);
    }
    
    private void OnOpen()
    {
    }
    
    private async void OnImport()
    {
        await ActionService.ShowImportWindowAsync();
    }
    
    private void OnSave()
    {
    }

    private void OnSaveAs()
    {
    }

    private async void OnExport()
    {
        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        Services.ExportPreview.PreviewBitmap = editorVM.GetBitmap();
        await ActionService.ShowExportWindowAsync();
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
        Services.ExportPreview.PreviewBitmap = editorVM.GetBitmap();
        await ActionService.ShowImagePropertiesWindowAsync();

        if (Services.RenderInvalidation.BitmapDirty)
        {
            editorVM.SetInitBitmap(Services.ExportPreview.PreviewBitmap!);
            Services.RenderInvalidation.BitmapDirty = false;
        }
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

    private static void OnStandart() => Services.WindowState.Current = Services.WindowState.PreviousWindowState;

    private static void OnFullScreen() => Services.WindowState.Current = WindowState.FullScreen;

    private static void OnLightTheme()
    {
        if (_settings.Theme is not "Light") _settings.Theme = "Light";
    }

    private static void OnDarkTheme()
    {
        if (_settings.Theme is not "Dark") _settings.Theme = "Dark";
    }

    private void OnCheckForUpdates()
    {
    }

    private void OnContactUs()
    {
    }

    private void OnAbout()
    {
    }
}