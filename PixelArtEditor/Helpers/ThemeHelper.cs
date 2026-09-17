using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Color = Avalonia.Media.Color;

namespace PixelArtEditor.Helpers;

public static class ThemeHelper
{
    public static Dictionary<string, string> ToHexDict(IResourceDictionary resources, IEnumerable<string> keys)
        => keys.Where(resources.ContainsKey).ToDictionary(k => k, k => ((Color)resources[k]!).ToString());

    public static void ApplyFromHexDict(IResourceDictionary resources, Dictionary<string, string> colors)
    {
        foreach (var (key, hex) in colors)
            resources[key] = Color.Parse(hex);
    }

    public static void ApplyFonts(IResourceDictionary resources, string appFontSource, 
        string appFontFamilyName, string headingFontSource, string headingFontFamilyName)
    {
        FontFamily AppFont;
        FontFamily HeadingFont;

        try
        {
            AppFont = LoadFontFromFile(appFontSource, appFontFamilyName);
            HeadingFont = LoadFontFromFile(headingFontSource, headingFontFamilyName);
        }
        catch (Exception)
        {
            Dispatcher.UIThread.InvokeAsync(
                async () => await ActionService.ShowErrorAsync(LocalizationService.Get("ThemeFontLoadError")));

            AppFont = new FontFamily("avares://PixelArtEditor/Styles/Fonts/Manrope-Medium.ttf#Manrope Medium");
            HeadingFont = new FontFamily("avares://PixelArtEditor/Styles/Fonts/Montserrat-SemiBold.ttf#Montserrat SemiBold");
        }

        resources["AppFont"] = AppFont;
        resources["HeadingFont"] = HeadingFont;
    }

    public static Bitmap LoadBitmap(string avaresRelativePath)
    {
        var uri = new Uri(avaresRelativePath, UriKind.Absolute);
        Stream? s = AssetLoader.Open(uri);
        return new Bitmap(s);
    }

    public static FontFamily LoadFontFromFile(string relativePath, string familyName)
    {
        var normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
        return new FontFamily($"avares://PixelArtEditor/{normalizedPath}#{familyName}");
    }
}