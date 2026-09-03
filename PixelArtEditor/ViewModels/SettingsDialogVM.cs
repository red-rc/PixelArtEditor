using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PixelArtEditor.AppServices;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Windows;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.ViewModels;

public class SettingsDialogVM : ReactiveObject
{
    private static ISettingsManager Settings => Services.Settings;

    public static IEnumerable<KeyValuePair<string, string>> LanguagePairs => ResourceManager.LanguageOptions;

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
        get => Settings.GridColor;
        set
        {
            if (Settings.GridColor == value) return;
            Settings.GridColor = value;
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

    public static IEnumerable<KeyValuePair<CheckerboardScale, string>> ScaleOptions
        => new Dictionary<CheckerboardScale, string>()
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
        get => Settings.AccentColor;
        set
        {
            if (Settings.AccentColor == value) return;
            Settings.AccentColor = value;
            this.RaisePropertyChanged();
        }
    }

    public List<string> ThemeOptions { get; set; } = [.. ResourceManager.ThemeOptions.Select(t => t.Name)];

    public string Theme
    {
        get => Settings.Theme;
        set
        {
            if (Settings.Theme == value) return;

            Settings.Theme = value;

            Dispatcher.UIThread.Post(_dialog.RestartWindow, DispatcherPriority.Background);
            this.RaisePropertyChanged();
        }
    }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    private readonly SettingsDialogWindow _dialog;

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public SettingsDialogVM(SettingsDialogWindow dialog)
    {
        ResetCommand = ReactiveCommand.Create(() => {
            Settings.Reset();
            OnClosing();
        }
        );
        CancelCommand = ReactiveCommand.Create(() =>
        {
            Settings.Load();
            dialog.Close();
        });
        SaveCommand = ReactiveCommand.Create(() =>
        {
            Settings.Save();
            dialog.Close();
        });

        _selectedTabIndex = 0;

        _dialog = dialog;
    }

    public void OnClosing()
    {
        foreach (var prop in typeof(ISettingsManager).GetProperties())
            this.RaisePropertyChanged(prop.Name);
    }
}