using Avalonia.Media;
using System.ComponentModel;

namespace PixelArtEditor.AppServices;

public interface ISettingsService
{
    string Language { get; set; }
    int GridMaxSize { get; set; }
    Color GridColor { get; set; }
    bool EnableGrid { get; set; }
    bool EnableAutosave { get; set; }
    int AutosaveFrequency { get; set; }

    string Theme { get; set; }
    Color AccentColor { get; set; }
    event PropertyChangedEventHandler? PropertyChanged;

    void Load();
    void Save();
}