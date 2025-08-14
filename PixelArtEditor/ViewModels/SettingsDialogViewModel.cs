using System.Reactive;
using Avalonia.Controls;
using PixelArtEditor.Other;
using ReactiveUI;

namespace PixelArtEditor.ViewModels;

public class SettingsDialogViewModel : ReactiveObject
{
    private readonly Settings _settings = Settings.GetInstance;
    
    public string Language
    {
        get => _settings.Language;
        set
        {
            if (_settings.Language == value) return;
            _settings.Language = value;
            this.RaisePropertyChanged();
        }
    }
    
    public string Theme 
    {
        get => _settings.Theme;
        set
        {
            if (_settings.Theme == value) return;
            _settings.Theme = value;
            this.RaisePropertyChanged();
        }
    }
    
    public int GridMaxSize
    {
        get => _settings.GridMaxSize;
        set
        {
            if (_settings.GridMaxSize == value) return;
            _settings.GridMaxSize = value;
            this.RaisePropertyChanged();
        }
    }
    
    public string GridColor
    {
        get => _settings.GridColor;
        set
        {
            if (_settings.GridColor == value) return;
            _settings.GridColor = value;
            this.RaisePropertyChanged();
        }
    }
    
    public bool EnableGrid
    {
        get => _settings.EnableGrid;
        set
        {
            if (_settings.EnableGrid == value) return;
            _settings.EnableGrid = value;
            this.RaisePropertyChanged();
        }
    }
    
    public bool EnableAutosave
    {
        get => _settings.EnableAutosave;
        set
        {
            if (_settings.EnableAutosave == value) return;
            _settings.EnableAutosave = value;
            this.RaisePropertyChanged();
        }
    }
    
    public int AutosaveFrequency
    {
        get => _settings.AutosaveFrequency;
        set
        {
            if (_settings.AutosaveFrequency == value) return;
            _settings.AutosaveFrequency = value;
            this.RaisePropertyChanged();
        }
    }
    
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public SettingsDialogViewModel(Window dialog)
    {
        ResetCommand = ReactiveCommand.Create(OnClosing);
        CancelCommand = ReactiveCommand.Create(() =>
        {
            OnClosing();
            
            dialog.Close();
        });
        SaveCommand = ReactiveCommand.Create(() =>
        {
            _settings.Save();
            dialog.Close();
        });
    }

    public void OnClosing()
    {
        _settings.Load();
            
        foreach (var prop in typeof(ISettings).GetProperties())
        {
            this.RaisePropertyChanged(prop.Name);
        }
    }
}