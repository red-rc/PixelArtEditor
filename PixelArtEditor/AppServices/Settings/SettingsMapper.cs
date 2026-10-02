namespace PixelArtEditor.AppServices.Settings;

public class SettingsMapper
{
    public static void ApplySnapshot(SettingsData data)
    {
        var settings = Services.Settings;

        settings.GridMaxSize = data.GridMaxSize;
        if (data.GridColor is not null) settings.GridColor = data.GridColor;
        settings.EnableGrid = data.EnableGrid;
        settings.ScaleCheckerboardWithCanvas = data.ScaleCheckerboardWithCanvas;
        settings.CheckerboardScale = data.CheckerboardScale;
        settings.InterpolationMode = data.InterpolationMode;
        settings.InterpolateOnlyWhenScalingDown = data.InterpolateOnlyWhenScalingDown;
        settings.EnableAutosave = data.EnableAutosave;
        settings.AutosaveFrequency = data.AutosaveFrequency;
        if (data.Language is not null) settings.Language = data.Language;
        if (data.AccentColor is not null) settings.AccentColor = data.AccentColor;
        if (data.Theme is not null) settings.Theme = data.Theme;
        if (data.Layout is not null) settings.Layout = data.Layout;
    }
}
