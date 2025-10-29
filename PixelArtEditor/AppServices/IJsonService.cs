namespace PixelArtEditor.AppServices;

public interface IJsonService
{
    T? Load<T>(string filePath);
    void Populate<T>(T target, string filePath);
    void Save<T>(T data, string filePath);
}
