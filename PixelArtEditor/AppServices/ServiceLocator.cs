namespace PixelArtEditor.AppServices;

public static class Services
{
    public static NavigationService Navigation { get; set; } = null!;
    public static ISettingsService Settings { get; set; } = null!;
    public static ImageDataService ImageData { get; set; } = null!;
    public static WindowStateService WindowState { get; set; } = null!;
}
