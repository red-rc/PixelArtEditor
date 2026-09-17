using Avalonia;
using Avalonia.Platform;
using PixelArtEditor.AppServices.Serialization;
using System;

namespace PixelArtEditor.AppServices;

public static class LocalizationService
{

    public static void SetLanguage(string langCode)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri($"avares://PixelArtEditor/Localization/{langCode}.yaml"));
            Load(YamlService.Load(stream));
        }
        catch (Exception)
        {
            using var stream = AssetLoader.Open(new Uri("avares://PixelArtEditor/Localization/en.yaml"));
            Load(YamlService.Load(stream));
        }
    }

    private static void Load(YamlData yamlData)
    {
        if (Application.Current == null)
            throw new InvalidOperationException($"{Get("ApplicationCurrentNull")}");

        foreach (var key in yamlData.LocalPairs.Keys)
            Application.Current.Resources[key] = yamlData.LocalPairs[key];
    }

    public static string Get(string key) =>
        Application.Current?.Resources[key] as string ?? key;
}
