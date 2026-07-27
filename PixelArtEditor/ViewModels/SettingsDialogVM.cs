using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.AppServices;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class SettingsDialogVM : ReactiveObject
{
    private static readonly ISettingsManager _settings = Services.Settings;

    public static IEnumerable<KeyValuePair<string, string>> LanguagePairs => ResourceManager.LanguageOptions;

    public KeyValuePair<string, string> Language
    {
        get => LanguagePairs.FirstOrDefault(i => i.Key == _settings.Language);
        set
        {
            if (Language.Equals(value)) return;
            _settings.Language = value.Key;
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
    
    public Color GridColor
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

    public Color AccentColor
    {
        get => _settings.AccentColor;
        set
        {
            if (_settings.AccentColor == value) return;
            _settings.AccentColor = value;
            this.RaisePropertyChanged();
        }
    }

    public List<string> ThemeOptions { get; set; } = [.. ResourceManager.ThemeOptions.Select(t => t.Name)];

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

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public SettingsDialogVM(Window dialog)
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

        _selectedTabIndex = 0;
    }

    public void OnClosing()
    {
        _settings.Load();
            
        foreach (var prop in typeof(ISettingsManager).GetProperties())
        {
            this.RaisePropertyChanged(prop.Name);
        }
    }
}