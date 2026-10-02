using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PixelArtEditor.AppServices.Serialization;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Dock;
using System;
using System.Collections.Generic;
using System.IO;

namespace PixelArtEditor.AppServices.Settings;

public sealed class SettingsManager : ReactiveObject, ISettingsManager
{
    public static SettingsManager GetInstance { get; } = new();

    private SettingsManager() => SetDefaults();

    private int _gridMaxSize;
    public int GridMaxSize
    {
        get => _gridMaxSize;
        set => this.RaiseAndSetIfChanged(ref _gridMaxSize, value);
    }

    private string _gridColor = null!;
    public string GridColor
    {
        get => _gridColor;
        set => this.RaiseAndSetIfChanged(ref _gridColor, value);
    }

    private bool _enableGrid;
    public bool EnableGrid
    {
        get => _enableGrid;
        set => this.RaiseAndSetIfChanged(ref _enableGrid, value);
    }

    private bool _enableAutosave;
    public bool EnableAutosave
    {
        get => _enableAutosave;
        set => this.RaiseAndSetIfChanged(ref _enableAutosave, value);
    }

    private int _autosaveFrequency;
    public int AutosaveFrequency
    {
        get => _autosaveFrequency;
        set => this.RaiseAndSetIfChanged(ref _autosaveFrequency, value);
    }

    private string _language = null!;
    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            LocalizationService.SetLanguage(value);
            this.RaiseAndSetIfChanged(ref _language, value);
        }
    }

    private bool _scaleCheckerboardWithCanvas;
    public bool ScaleCheckerboardWithCanvas
    {
        get => _scaleCheckerboardWithCanvas;
        set => this.RaiseAndSetIfChanged(ref _scaleCheckerboardWithCanvas, value);
    }

    private CheckerboardScale _checkerboardScale;
    public CheckerboardScale CheckerboardScale
    {
        get => _checkerboardScale;
        set => this.RaiseAndSetIfChanged(ref _checkerboardScale, value);
    }

    private BitmapInterpolationMode _interpolationMode;
    public BitmapInterpolationMode InterpolationMode
    {
        get => _interpolationMode;
        set => this.RaiseAndSetIfChanged(ref _interpolationMode, value);
    }

    private bool _interpolateOnlyWhenScalingDown;
    public bool InterpolateOnlyWhenScalingDown
    {
        get => _interpolateOnlyWhenScalingDown;
        set => this.RaiseAndSetIfChanged(ref _interpolateOnlyWhenScalingDown, value);
    }

    private string _accentColor = null!;
    public string AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor == value) return;

            foreach (var theme in ResourceManager.ThemeOptions)
                theme.ChangeAccentColor(value);

            this.RaiseAndSetIfChanged(ref _accentColor, value);
        }
    }

    private string _theme = null!;
    public string Theme
    {
        get => _theme;
        set
        {
            if (_theme == value) return;
            Array.Find(ResourceManager.ThemeOptions, x => x.Name == value)?.Apply();
            this.RaiseAndSetIfChanged(ref _theme, value);
        }
    }

    private List<PanelLayout> _layout = [];
    public List<PanelLayout> Layout
    {
        get => _layout;
        set => this.RaiseAndSetIfChanged(ref _layout, value);
    }

    public SettingsData GetSnapshot()
    {
        return new SettingsData
        {
            GridMaxSize = GridMaxSize,
            GridColor = GridColor,
            EnableGrid = EnableGrid,
            EnableAutosave = EnableAutosave,
            AutosaveFrequency = AutosaveFrequency,
            Language = Language,
            ScaleCheckerboardWithCanvas = ScaleCheckerboardWithCanvas,
            CheckerboardScale = CheckerboardScale,
            InterpolateOnlyWhenScalingDown = InterpolateOnlyWhenScalingDown,
            InterpolationMode = InterpolationMode,
            AccentColor = AccentColor,
            Theme = Theme,
            Layout = Layout
        };
    }

    public void Load()
    {
        try
        {
            var loaded = JsonService.Load(ResourceManager.SettingsPath, AppJsonContext.Default.SettingsData)
                ?? throw new InvalidDataException();

            GridMaxSize = loaded.GridMaxSize;
            if (loaded.GridColor is not null) GridColor = loaded.GridColor;
            EnableGrid = loaded.EnableGrid;
            ScaleCheckerboardWithCanvas = loaded.ScaleCheckerboardWithCanvas;
            CheckerboardScale = loaded.CheckerboardScale;
            InterpolationMode = loaded.InterpolationMode;
            InterpolateOnlyWhenScalingDown = loaded.InterpolateOnlyWhenScalingDown;
            EnableAutosave = loaded.EnableAutosave;
            AutosaveFrequency = loaded.AutosaveFrequency;
            if (loaded.Language is not null) Language = loaded.Language;
            if (loaded.AccentColor is not null) AccentColor = loaded.AccentColor;
            if (loaded.Theme is not null) Theme = loaded.Theme;
            if (loaded.Layout is not null) Layout = loaded.Layout;
        }
        catch (Exception ex)
        {
            try { Save(); }
            catch { Dispatcher.UIThread.InvokeAsync(async () => await ActionService.ShowError(ex.ToString())); }
        }
    }

    private void SetDefaults()
    {
        Language = "en";
        GridMaxSize = 32;
        GridColor = "#7f7f7f";
        EnableGrid = true;
        ScaleCheckerboardWithCanvas = false;
        CheckerboardScale = CheckerboardScale.Scale4;
        InterpolationMode = BitmapInterpolationMode.HighQuality;
        InterpolateOnlyWhenScalingDown = true;
        EnableAutosave = true;
        AutosaveFrequency = 10;
        AccentColor = "#1e90ff";
        Theme = "System";
        Layout = ResourceManager.DefaultLayout;
    }

    public void Save()
        => JsonService.Save(GetSnapshot(), ResourceManager.SettingsPath, AppJsonContext.Default.SettingsData);

    public void Reset()
    {
        SetDefaults();
        Save();
    }
}