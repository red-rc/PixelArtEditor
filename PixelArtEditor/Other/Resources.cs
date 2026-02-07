using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.Styles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.Other;

public static class Resources
{
    public const string ConfigPath = "config.json";
    public const string ThemesPath = "themes.json";

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

        try
        {
            loadedThemes = JsonService.Load<BaseTheme[]>(ThemesPath);
        }
        catch
        {
            loadedThemes = null;
        }

        if (loadedThemes is null || loadedThemes.Length == 0)
            loadedThemes = CreateDefaultThemes();

        ThemeOptions = loadedThemes;

        List<string> langKeys = [.. Directory.GetFiles("Localization", "*.yaml")
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
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => await ShowError($"Error: {ex.Message}"));
            return [];
        }
    }

    private static async Task ShowError(string message)
    {
        Window? window = null;
        window = new Window
        {
            Title = "Error",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new StackPanel
            {
                Margin = new Thickness(10),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0,0,0,10)
                    },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Command = ReactiveUI.ReactiveCommand.Create(() => window!.Close())
                    }
                }
            }
        };

        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is not null)
            await window.ShowDialog(owner);
        else
            window.Show();
    }
}