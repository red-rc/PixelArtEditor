using Avalonia.Styling;
using System.Collections.Generic;

namespace PixelArtEditor.Styles;

public static class ThemeSerializer
{
    public static ThemeData ToData(BaseTheme theme) => new()
    {
        Name = theme.Name,
        Variant = theme.Variant.ToString(),
        Colors = new Dictionary<string, string>(theme.Resources.Colors),
        AppFontSource = theme.Resources.AppFontSource,
        AppFontFamilyName = theme.Resources.AppFontFamilyName,
        HeadingFontSource = theme.Resources.HeadingFontSource,
        HeadingFontFamilyName = theme.Resources.HeadingFontFamilyName
    };

    public static void ApplyData(BaseTheme theme, ThemeData data)
    {
        theme.Name = data.Name;
        theme.Variant = data.Variant switch
        {
            "Dark" => ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };

        theme.Resources.Colors.Clear();
        foreach (var (key, value) in data.Colors)
            theme.Resources.Colors[key] = value;

        theme.Resources.AppFontSource = data.AppFontSource;
        theme.Resources.AppFontFamilyName = data.AppFontFamilyName;
        theme.Resources.HeadingFontSource = data.HeadingFontSource;
        theme.Resources.HeadingFontFamilyName = data.HeadingFontFamilyName;
    }
}
