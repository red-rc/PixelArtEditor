using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices.Serialization;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Dock;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PixelArtEditor.AppServices;

public sealed class SettingsManager : ISettingsManager
{
    public static SettingsManager GetInstance { get; } = new();

    private SettingsManager() => SetDefaults();

    public int GridMaxSize { get; set; }
    public Color GridColor { get; set; }
    public bool EnableGrid { get; set; }
    public bool EnableAutosave { get; set; }
    public int AutosaveFrequency { get; set; }

    private string _language = null!;
    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            LocalizationService.SetLanguage(value);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        }
    }

    private bool _scaleCheckerboardWithCanvas;
    public bool ScaleCheckerboardWithCanvas
    {
        get => _scaleCheckerboardWithCanvas;
        set
        {
            if (_scaleCheckerboardWithCanvas == value) return;
            _scaleCheckerboardWithCanvas = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScaleCheckerboardWithCanvas)));
        }
    }

    private CheckerboardScale _checkerboardScale;
    public CheckerboardScale CheckerboardScale
    {
        get => _checkerboardScale;
        set
        {
            if (_checkerboardScale == value) return;
            _checkerboardScale = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckerboardScale)));
        }
    }

    private BitmapInterpolationMode _interpolationMode;
    public BitmapInterpolationMode InterpolationMode
    {
        get => _interpolationMode;
        set
        {
            if (_interpolationMode == value) return;
            _interpolationMode = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InterpolationMode)));
        }
    }

    private bool _interpolateOnlyWhenScalingDown;
    public bool InterpolateOnlyWhenScalingDown
    {
        get => _interpolateOnlyWhenScalingDown;
        set
        {
            if (_interpolateOnlyWhenScalingDown == value) return;
            _interpolateOnlyWhenScalingDown = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InterpolateOnlyWhenScalingDown)));
        }
    }

    private Color _accentColor;
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor == value) return;
            _accentColor = value;
            foreach (var theme in ResourceManager.ThemeOptions)
                theme.ChangeAccentColor(value);
                
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
            Array.Find(ResourceManager.ThemeOptions, x => x.Name == value)?.Apply();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
        }
    }

    private List<PanelLayout> _layout = [];
    public List<PanelLayout> Layout
    {
        get => _layout;
        set
        {
            if (_layout == value) return;
            _layout = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Layout)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Load()
    {
        try { JsonService.Populate(this, ResourceManager.ConfigPath); }
        catch (Exception) { JsonService.Save(this, ResourceManager.ConfigPath); }
    }

    private void SetDefaults()
    {
        Language = "en";
        GridMaxSize = 32;
        GridColor = Color.Parse("#7f7f7f");
        EnableGrid = true;
        ScaleCheckerboardWithCanvas = false;
        CheckerboardScale = CheckerboardScale.Scale4;
        InterpolationMode = BitmapInterpolationMode.None;
        InterpolateOnlyWhenScalingDown = true;
        EnableAutosave = true;
        AutosaveFrequency = 10;
        AccentColor = Color.Parse("DodgerBlue");
        Theme = "System";
        Layout = ResourceManager.DefaultLayout;
    }

    public void Save() => JsonService.Save(this, ResourceManager.ConfigPath);
    public void Reset()
    {
        SetDefaults();
        Save();
    }
}
