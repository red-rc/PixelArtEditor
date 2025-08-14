using System;
using System.IO;

namespace PixelArtEditor.Other;

public sealed class Settings : ISettings
{
    public static Settings GetInstance { get; } = new(true);

    private Settings(bool load = false)
    {
        if (!load) return;
        Load();
    }
    
    public string Language { get; set; } = null!;
    public string Theme { get; set; } = null!;
    public int GridMaxSize { get; set; }
    public string GridColor { get; set; } = null!;
    public bool EnableGrid { get; set; }
    public bool EnableAutosave { get; set; }
    public int AutosaveFrequency { get; set; }

    public void Load()
    {
        try
        {
            var json = File.ReadAllText(Resources.ConfigPath);
            Newtonsoft.Json.JsonConvert.PopulateObject(json, this);
        }
        catch (Exception)
        {
            Language = "en";
            Theme = "system";
            GridMaxSize = 32;
            GridColor = "#7f7f7f";
            EnableGrid = true;
            EnableAutosave = true;
            AutosaveFrequency = 10;

            JsonService.Save(this, Resources.ConfigPath);
        }
    }

    public void Save()
    {
        JsonService.Save(this, Resources.ConfigPath);
    }
}
