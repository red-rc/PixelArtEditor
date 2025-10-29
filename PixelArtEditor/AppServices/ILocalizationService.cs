namespace PixelArtEditor.AppServices;

public interface ILocalizationService
{
    void SetLanguage(string langCode);
    void SetDefaults();
}
