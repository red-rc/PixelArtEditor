namespace PixelArtEditor.AppServices;

public static class Services
{
    public static NavigationService Navigation { get; set; } = null!;
    public static ISettingsService Settings { get; set; } = null!;
    public static ExportPreviewService ExportPreview { get; set; } = null!;
    public static WindowStateService WindowState { get; set; } = null!;
    public static RenderInvalidationService RenderInvalidation { get; set; } = null!;
}
