using System;
using System.ComponentModel;
using Avalonia.Media;
using PixelArtEditor.Other;

namespace PixelArtEditor.AppServices;

public sealed class SettingsService : ISettingsService
{
    public static SettingsService GetInstance { get; } = new(true);

    private SettingsService(bool load = false)
    {
        if (!load) return;
        Load();
    }

    private string _language = null!;
    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            Services.Localization.SetLanguage(value);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        }
    }
    public int GridMaxSize { get; set; }
    public Color GridColor { get; set; }
    public bool EnableGrid { get; set; }
    public bool EnableAutosave { get; set; }
    public int AutosaveFrequency { get; set; }

    private Color _accentColor;
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor == value) return;
            _accentColor = value;
            foreach (var theme in Resources.ThemeOptions)
            {
                theme.ChangeAccentColor(value);
            }
                
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccentColor)));
        }
    }

    private string _theme = null!;
    public string Theme
    {
        get => _theme;
        set
        {
            if (_theme == value) return;
            _theme = value;
            Array.Find(Resources.ThemeOptions, x => x.Name == value)?.Apply();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Load()
    {
        try
        {
            Services.Json.Populate(this, Resources.ConfigPath);
        }
        catch (Exception)
        {
            SetDefaults();
            Services.Json.Save(this, Resources.ConfigPath);
        }
    }

    private void SetDefaults()
    {
        Language = "en";
        GridMaxSize = 32;
        GridColor = Color.Parse("#7f7f7f");
        EnableGrid = true;
        EnableAutosave = true;
        AutosaveFrequency = 10;
        AccentColor = Color.Parse("DodgerBlue");
        Theme = "System";
    }

    public void Save()
    {
        Services.Json.Save(this, Resources.ConfigPath);
    }
}
