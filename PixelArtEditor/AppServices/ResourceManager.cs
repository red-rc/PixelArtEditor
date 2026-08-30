using Avalonia.Threading;
using PixelArtEditor.AppServices.Serialization;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.Models.Dock;
using PixelArtEditor.Styles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PixelArtEditor.AppServices;

public static class ResourceManager
{
    public const string ConfigPath = "config.json";
    public const string ThemesPath = "themes.json";

    public static List<PanelLayout> DefaultLayout { get; } =
    [
        new() { Name = "OptionsPanel", Row = 0, Col = 0 },
        new() { Name = "LayerPanel",   Row = 1, Col = 2 },
        new() { Name = "Toolbar",      Row = 1, Col = 0 },
        new() { Name = "CanvasPanel",  Row = 1, Col = 1 }
    ];

    public static BaseTheme[] ThemeOptions { get; private set; } = [];
    public static Dictionary<string, string> LanguageOptions { get; private set; } = [];

    public static readonly Dictionary<string, string> LanguageNames = new()
    {
        { "en", "English" },
        { "es", "Español" },
        { "fr", "Français" },
        { "de", "Deutsch" },
        { "it", "Italiano" },
        { "pt", "Português" },
        { "zh", "中文" },
        { "ja", "日本語" },
        { "uk", "Українська" },
        { "ko", "한국어" }
    };

    public static void Initialize()
    {
        BaseTheme[]? loadedThemes;

        try { loadedThemes = JsonService.Load<BaseTheme[]>(ThemesPath); }
        catch { loadedThemes = null; }

        if (loadedThemes is null || loadedThemes.Length == 0)
            loadedThemes = CreateDefaultThemes();

        ThemeOptions = loadedThemes;

        if (!Directory.Exists("Localization"))
            Directory.CreateDirectory("Localization");

        var files = Directory.GetFiles("Localization", "*.yaml");

        if (files.Length == 0)
        {
            LocalizationService.SetDefaults();
            files = Directory.GetFiles("Localization", "*.yaml");
        }

        List<string> langKeys = [.. files
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Where(x => x is not null && !string.IsNullOrEmpty(x) && LanguageNames.ContainsKey(x!))];

        LanguageOptions = langKeys.ToDictionary(x => x, y => LanguageNames[y]);
    }

    private static BaseTheme[] CreateDefaultThemes()
    {
        try
        {
            var defaults = new BaseTheme[]
            {
                new DarkTheme(),
                new LightTheme(),
                new GrayTheme(),
                new SystemTheme()
            };

            JsonService.Save(defaults, ThemesPath);

            return defaults;
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.InvokeAsync(async () => await ActionService.ShowErrorAsync(ex.Message));
            return [];
        }
    }
}