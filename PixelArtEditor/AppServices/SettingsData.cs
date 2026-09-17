using Avalonia.Media.Imaging;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Dock;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices;

/// <summary>
/// DTO для серіалізації налаштувань у JSON.
/// Розділений з <see cref="SettingsManager"/> (синглтон з побічними ефектами в сеттерах),
/// щоб System.Text.Json міг коректно десеріалізувати config.json.
/// </summary>
public sealed class SettingsData
{
    public int GridMaxSize { get; set; }
    public string? GridColor { get; set; }
    public bool EnableGrid { get; set; }
    public bool EnableAutosave { get; set; }
    public int AutosaveFrequency { get; set; }

    public string? Language { get; set; }

    public bool ScaleCheckerboardWithCanvas { get; set; }
    public CheckerboardScale CheckerboardScale { get; set; }
    public bool InterpolateOnlyWhenScalingDown { get; set; }
    public BitmapInterpolationMode InterpolationMode { get; set; }

    public string? AccentColor { get; set; }
    public string? Theme { get; set; }
    public List<PanelLayout>? Layout { get; set; }
}