using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.Models.Dock;
using System.Collections.Generic;
using System.ComponentModel;

namespace PixelArtEditor.AppServices;

public interface ISettingsManager
{
    string Language { get; set; }
    int GridMaxSize { get; set; }
    Color GridColor { get; set; }
    bool EnableGrid { get; set; }
    bool ScaleCheckerboardWithCanvas { get; set; }
    CheckerboardScale CheckerboardScale { get; set; }
    BitmapInterpolationMode InterpolationMode { get; set; }
    bool EnableAutosave { get; set; }
    int AutosaveFrequency { get; set; }

    string Theme { get; set; }
    Color AccentColor { get; set; }

    List<PanelLayout> Layout { get; set; }
    event PropertyChangedEventHandler? PropertyChanged;

    void Load();
    void Save();
    void Reset();
}