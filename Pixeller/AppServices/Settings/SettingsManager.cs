using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Pixeller.AppServices.Serialization;
using Pixeller.AppServices.Shell;
using Pixeller.Models.Canvas;
using Pixeller.Models.Dock;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pixeller.AppServices.Settings;

public sealed partial class SettingsManager : ReactiveObject, ISettingsManager
{
    public static SettingsManager GetInstance { get; } = new();

    private SettingsManager() => SetDefaults();

    [Reactive] public partial int GridMaxSize { get; set; }

    private string _gridColor = null!;
    public string GridColor
    {
        get => _gridColor;
        set => this.RaiseAndSetIfChanged(ref _gridColor, value);
    }

    [Reactive] public partial bool EnableGrid { get; set; }
    [Reactive] public partial bool EnableAutosave { get; set; }
    [Reactive] public partial int AutosaveFrequency { get; set; }

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

    [Reactive] public partial bool ScaleCheckerboardWithCanvas { get; set; }
    [Reactive] public partial CheckerboardScale CheckerboardScale { get; set; }
    [Reactive] public partial BitmapInterpolationMode InterpolationMode { get; set; }
    [Reactive] public partial bool InterpolateOnlyWhenScalingDown { get; set; }

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
