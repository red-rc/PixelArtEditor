using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Windows;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class SettingsDialogVM : ReactiveObject
{
    private static ISettingsManager Settings => Services.Settings;

    public static Dictionary<string, string> LanguagePairs => ResourceManager.LanguageOptions;

    public KeyValuePair<string, string> Language
    {
        get => LanguagePairs.FirstOrDefault(i => i.Key == Settings.Language);
        set
        {
            if (Language.Equals(value)) return;
            Settings.Language = value.Key;
            this.RaisePropertyChanged();
        }
    }

    public int GridMaxSize
    {
        get => Settings.GridMaxSize;
        set
        {
            if (Settings.GridMaxSize == value) return;
            Settings.GridMaxSize = value;
            this.RaisePropertyChanged();
        }
    }
    
    public Color GridColor
    {
        get => ColorHelper.HexToColor(Settings.GridColor);
        set
        {
            if (Settings.GridColor == value.ToString()) return;
            Settings.GridColor = value.ToString();
            this.RaisePropertyChanged();
        }
    }
    
    public bool EnableGrid
    {
        get => Settings.EnableGrid;
        set
        {
            if (Settings.EnableGrid == value) return;
            Settings.EnableGrid = value;
            this.RaisePropertyChanged();
        }
    }

    public bool ScaleCheckerboardWithCanvas
    {
        get => Settings.ScaleCheckerboardWithCanvas;
        set
        {
            if (Settings.ScaleCheckerboardWithCanvas == value) return;
            Settings.ScaleCheckerboardWithCanvas = value;
            this.RaisePropertyChanged();
        }
    }

    public static Dictionary<CheckerboardScale, string> ScaleOptions => new()
    {
        { CheckerboardScale.Scale1, "1" },
        { CheckerboardScale.Scale2, "2" },
        { CheckerboardScale.Scale4, "4" },
        { CheckerboardScale.Scale8, "8" },
        { CheckerboardScale.Scale16, "16" },
        { CheckerboardScale.Scale32, "32" },
        { CheckerboardScale.Scale64, "64" }
    };

    public KeyValuePair<CheckerboardScale, string> Scale
    {
        get => ScaleOptions.FirstOrDefault(i => i.Key == Settings.CheckerboardScale);
        set
        {
            if (Scale.Equals(value)) return;
            Settings.CheckerboardScale = value.Key;
            this.RaisePropertyChanged();
        }
    }

    public static IEnumerable<KeyValuePair<BitmapInterpolationMode, string>> InterpolationOptions
        => new Dictionary<BitmapInterpolationMode, string>()
    {
        { BitmapInterpolationMode.None, LocalizationService.Get("InterpolationNone") },
        { BitmapInterpolationMode.LowQuality, LocalizationService.Get("InterpolationLow") },
        { BitmapInterpolationMode.MediumQuality, LocalizationService.Get("InterpolationMedium") },
        { BitmapInterpolationMode.HighQuality, LocalizationService.Get("InterpolationHigh") }
    };

    public KeyValuePair<BitmapInterpolationMode, string> InterpolationMode
    {
        get => InterpolationOptions.FirstOrDefault(i => i.Key == Settings.InterpolationMode);
        set
        {
            if (InterpolationMode.Equals(value)) return;
            Settings.InterpolationMode = value.Key;
            this.RaisePropertyChanged();
        }
    }

    public bool InterpolateOnlyWhenScalingDown
    {
        get => Settings.InterpolateOnlyWhenScalingDown;
        set
        {
            if (Settings.InterpolateOnlyWhenScalingDown == value) return;
            Settings.InterpolateOnlyWhenScalingDown = value;
            this.RaisePropertyChanged();
        }
    }

    public bool EnableAutosave
    {
        get => Settings.EnableAutosave;
        set
        {
            if (Settings.EnableAutosave == value) return;
            Settings.EnableAutosave = value;
            this.RaisePropertyChanged();
        }
    }
    
    public int AutosaveFrequency
    {
        get => Settings.AutosaveFrequency;
        set
        {
            if (Settings.AutosaveFrequency == value) return;
            Settings.AutosaveFrequency = value;
            this.RaisePropertyChanged();
        }
    }

    public Color AccentColor
    {
        get => ColorHelper.HexToColor(Settings.AccentColor);
        set
        {
            if (Settings.AccentColor == value.ToString()) return;
            Settings.AccentColor = value.ToString();
            this.RaisePropertyChanged();
        }
    }

    public static Dictionary<string, string> ThemeOptions 
        => ResourceManager.ThemeOptions.ToDictionary(t => t.Name, t => LocalizationService.Get(t.Name));

    public KeyValuePair<string, string> Theme
    {
        get => new(Settings.Theme, LocalizationService.Get(Settings.Theme));
        set
        {
            if (Settings.Theme == value.Key) return;
            Settings.Theme = value.Key;
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

    private bool _savePress = false;

    public SettingsDialogVM(SettingsDialogWindow dialog)
    {
        ResetCommand = ReactiveCommand.Create(() => {
            Settings.Reset();
            OnClosing();
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);

        SaveCommand = ReactiveCommand.Create(() =>
        {
            Settings.Save();
            _savePress = true;
            dialog.Close();
        });

        _selectedTabIndex = 0;
    }

    public void OnClosing()
    {
        foreach (var prop in typeof(ISettingsManager).GetProperties())
            this.RaisePropertyChanged(prop.Name);

        if (!_savePress)
            Settings.Load();

        _savePress = false;
    }
}