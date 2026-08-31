using Avalonia.Controls;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Shell;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Reactive.Linq;

namespace PixelArtEditor.ViewModels;

public class MenuCommandsVM : ReactiveObject
{
    private static ISettingsManager Settings => Services.Settings;
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
    public ReactiveCommand<RxVoid, RxVoid> WindowStateCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> LightThemeCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> DarkThemeCommand { get; }
    
    public ReactiveCommand<RxVoid, RxVoid> CheckForUpdatesCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ReportCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ContactUsCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> AboutCommand { get; }

    private string _windowStateHeader = LocalizationService.Get("MenuWindowStateF");
    public string WindowStateHeader
    {
        get => _windowStateHeader;
        set => this.RaiseAndSetIfChanged(ref _windowStateHeader, value);
    }

    public MenuCommandsVM()
    {
        var isDocumentOpen = Services
            .Navigation
            .WhenCurrentViewChanges()
            .Select(view => view is EditorVM)
            .DistinctUntilChanged();

        var isFullScreen = Services.WindowState
            .WhenAnyValue(x => x.Current)
            .Select(state => state == WindowState.FullScreen)
            .DistinctUntilChanged();

        Services.Settings.PropertyChanged += OnSettingsPropertyChanged;

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
        WindowStateCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var isFullscreen = await isFullScreen.FirstAsync();
            OnWindowState(isFullscreen);
        });
        LightThemeCommand = ReactiveCommand.Create(OnLightTheme);
        DarkThemeCommand = ReactiveCommand.Create(OnDarkTheme);
        
        CheckForUpdatesCommand = ReactiveCommand.Create(OnCheckForUpdates);
        ReportCommand = ReactiveCommand.Create(() => OpenUrl("https://github.com/red-rc/PixelArtEditor/issues"));
        ContactUsCommand = ReactiveCommand.Create(() => OpenUrl("https://mail.google.com/mail/u/0/?to=redthar7@gmail.com&fs=1&tf=cm"));
        AboutCommand = ReactiveCommand.Create(() => OpenUrl("https://github.com/red-rc/PixelArtEditor"));
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsManager.Language))
        {
            WindowStateHeader = LocalizationService.Get(
                Services.WindowState.Current == WindowState.FullScreen
                    ? "MenuWindowStateW"
                    : "MenuWindowStateF");
        }
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
        editorVM.ZoomBy(1/1.2f);
    }

    private static void OnResetZoom()
    {
        if (Services.Navigation.GetViewModel() is not EditorVM editorVM) return;
        editorVM.Scale = editorVM.BaseScale;
        editorVM.Offset = Vector2.Zero;
    }

    private static void OnResetLayout()
    {
        Settings.Layout = ResourceManager.DefaultLayout;
        Settings.Save();
    }

    private void OnWindowState(bool isFullscreen) 
    {
        if (isFullscreen)
        {
            Services.WindowState.Current = Services.WindowState.PrevWindowState;
            WindowStateHeader = LocalizationService.Get("MenuWindowStateF");
        }
        else
        {
            Services.WindowState.Current = WindowState.FullScreen;
            WindowStateHeader = LocalizationService.Get("MenuWindowStateW");
        }
    }

    private static void OnLightTheme()
    {
        if (Settings.Theme is not "Light") Settings.Theme = "Light";
        Settings.Save();
    }

    private static void OnDarkTheme()
    {
        if (Settings.Theme is not "Dark") Settings.Theme = "Dark";
        Settings.Save();
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