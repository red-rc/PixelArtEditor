using System;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using PixelArtEditor.Services;
using ReactiveUI;

namespace PixelArtEditor.ViewModels;

public class MenuCommandsViewModel : ReactiveObject
{
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
    public ReactiveCommand<Unit, Unit> WhiteThemeCommand { get; }
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
        White Theme
        Dark Theme

        Check For Updates
        Contact Us
        About
     */

    public MenuCommandsViewModel(IAppServices services)
    {
        var isDocumentOpen = services
            .Navigation
            .WhenCurrentViewChanges()
            .Select(view => view is EditorViewModel)
            .DistinctUntilChanged();
        
        CreateCommand = ReactiveCommand.CreateFromTask(() => services.Actions.ShowCreateWindowAsync(services));
        OpenCommand = ReactiveCommand.Create(OnOpen);
        ImportCommand = ReactiveCommand.Create(OnImport);
        SaveCommand = ReactiveCommand.Create(OnSave, isDocumentOpen); 
        SaveAsCommand = ReactiveCommand.Create(OnSaveAs, isDocumentOpen);
        ExportCommand = ReactiveCommand.Create(OnExport, isDocumentOpen);
        LastAutosaveCommand = ReactiveCommand.Create(OnLastAutosave); //TODO: Add condition if last save exists
        ExitCommand = ReactiveCommand.Create(() => services.Navigation.NavigateTo(new StartMenuViewModel(services)), isDocumentOpen);
    
        UndoCommand = ReactiveCommand.Create(OnUndo, isDocumentOpen);
        RedoCommand = ReactiveCommand.Create(OnRedo, isDocumentOpen);
        ImagePropertiesCommand = ReactiveCommand.Create(OnImageProperties, isDocumentOpen);
        SettingsCommand = ReactiveCommand.CreateFromTask(() => services.Actions.ShowSettingsWindowAsync(services));
        
        ZoomInCommand = ReactiveCommand.Create(() => OnZoomIn(services), isDocumentOpen);
        ZoomOutCommand = ReactiveCommand.Create(() => OnZoomOut(services), isDocumentOpen);
        ResetZoomCommand = ReactiveCommand.Create(() => OnResetZoom(services), isDocumentOpen);
        WhiteThemeCommand = ReactiveCommand.Create(OnWhiteTheme);
        DarkThemeCommand = ReactiveCommand.Create(OnDarkTheme);
        
        CheckForUpdatesCommand = ReactiveCommand.Create(OnCheckForUpdates);
        ContactUsCommand = ReactiveCommand.Create(OnContactUs);
        AboutCommand = ReactiveCommand.Create(OnAbout);
    }
    
    private void OnOpen()
    {
    }
    
    private void OnImport()
    {
    }
    
    private void OnExport()
    {
    }
    
    private void OnSave()
    {
    }

    private void OnSaveAs()
    {
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

    private void OnImageProperties()
    {
    }

    private void OnZoomIn(IAppServices services)
    {
        if (services.Navigation.GetViewModel() is not EditorViewModel editorViewModel) return;
        editorViewModel.Scale = Math.Min(editorViewModel.Scale * 1.2, editorViewModel.MaxScale);
    }

    private void OnZoomOut(IAppServices services)
    {
        if (services.Navigation.GetViewModel() is not EditorViewModel editorViewModel) return;
        editorViewModel.Scale = Math.Max(editorViewModel.Scale * 0.8, editorViewModel.MinScale);
    }

    private void OnResetZoom(IAppServices services)
    {
        if (services.Navigation.GetViewModel() is not EditorViewModel editorViewModel) return;
        editorViewModel.Scale = editorViewModel.BaseScale;
        editorViewModel.Offset = new Vector2(0, 0);
    }

    private void OnWhiteTheme()
    {
    }

    private void OnDarkTheme()
    {
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