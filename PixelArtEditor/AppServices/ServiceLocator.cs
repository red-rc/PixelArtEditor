namespace PixelArtEditor.AppServices;

public static class Services
{
    public static IDialogService Dialogs { get; set; } = null!;
    public static INavigationService Navigation { get; set; } = null!;
    public static IActionsService Actions { get; set; } = null!;
    public static IBitmapService Bitmap { get; set; } = null!;
    public static IJsonService Json { get; set; } = null!;
    public static IYamlService Yaml { get; set; } = null!;
    public static ISettingsService Settings { get; set; } = null!;
    public static ILocalizationService Localization { get; set; } = null!;
}
