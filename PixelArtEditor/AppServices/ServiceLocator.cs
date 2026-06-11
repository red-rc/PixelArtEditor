using PixelArtEditor.AppServices.Image;
using PixelArtEditor.AppServices.Shell;

namespace PixelArtEditor.AppServices;

public static class Services
{
    public static NavigationService Navigation { get; set; } = null!;
    public static ISettingsService Settings { get; set; } = null!;
    public static ModelManager ModelData { get; set; } = null!;
    public static WindowStateManager WindowState { get; set; } = null!;
}
