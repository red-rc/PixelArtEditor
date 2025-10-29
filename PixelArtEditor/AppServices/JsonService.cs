using System.IO;

namespace PixelArtEditor.AppServices;

public class JsonService : IJsonService
{
    public T? Load<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;
            
        var jsonString = File.ReadAllText(filePath);
        return string.IsNullOrWhiteSpace(jsonString) ? default : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonString);
    }
    
    public void Populate<T>(T target, string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");

        var jsonString = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(jsonString)) throw new InvalidDataException("Config file is empty.");

        if (target != null) Newtonsoft.Json.JsonConvert.PopulateObject(jsonString, target);
    }
    
    public void Save<T>(T data, string filePath)
    {
        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

        var tmp = filePath + ".tmp";
        File.WriteAllText(tmp, jsonString);
        
        if (File.Exists(filePath)) File.Delete(filePath);

        File.Move(tmp, filePath);
    }
}