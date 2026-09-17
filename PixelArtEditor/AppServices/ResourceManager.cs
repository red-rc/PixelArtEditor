using Avalonia.Platform;
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
    public const string ThemesPath = "Styles/themes.json";

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
        List<BaseTheme> themes;

        ThemeData[]? loadedData;
        try { loadedData = JsonService.Load(ThemesPath, AppJsonContext.Default.ThemeDataArray); }
        catch { loadedData = null; }

        if (loadedData is null || loadedData.Length == 0)
        {
            themes = [DefaultThemes.CreateDark(), DefaultThemes.CreateLight(),
                DefaultThemes.CreateGray(), DefaultThemes.CreateSystem()];

            var toSave = themes.Select(ThemeSerializer.ToData).ToArray();
            try { JsonService.Save(toSave, ThemesPath, AppJsonContext.Default.ThemeDataArray); }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () => await ActionService.ShowErrorAsync(ex.Message));
            }
        }
        else
        {
            themes = [];
            foreach (var data in loadedData)
            {
                var theme = new BaseTheme();
                ThemeSerializer.ApplyData(theme, data);
                themes.Add(theme);
            }
        }

        ThemeOptions = [.. themes];

        var langKeys = AssetLoader.GetAssets(new Uri("avares://PixelArtEditor/Localization/"), null)
            .Select(uri => Path.GetFileNameWithoutExtension(uri.AbsolutePath))
            .Where(LanguageNames.ContainsKey)
            .ToList();

        LanguageOptions = langKeys.ToDictionary(x => x, y => LanguageNames[y]);
    }
}