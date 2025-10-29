using System.Collections.Generic;

namespace PixelArtEditor.AppServices;

public interface IYamlService
{
    Dictionary<string, string> Load(string path);
    void Save(Dictionary<string, string> data, string path);
}
