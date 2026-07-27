using Avalonia.Media;
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
    bool EnableAutosave { get; set; }
    int AutosaveFrequency { get; set; }

    string Theme { get; set; }
    Color AccentColor { get; set; }

    List<PanelLayout> Layout { get; set; }
    event PropertyChangedEventHandler? PropertyChanged;

    void Load();
    void Save();
}