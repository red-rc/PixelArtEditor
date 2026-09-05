using Newtonsoft.Json;
using System.IO;

namespace PixelArtEditor.AppServices.Serialization;

public static class JsonService
{
    public static T? Load<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;
            
        var jsonString = File.ReadAllText(filePath);
        return string.IsNullOrWhiteSpace(jsonString) ? default : JsonConvert.DeserializeObject<T>(jsonString);
    }
    
    public static void Populate<T>(T target, string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"{LocalizationService.Get("FileNotFound")}: {filePath}");

        var jsonString = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(jsonString)) throw new InvalidDataException($"{LocalizationService.Get("EmptyConfig")}");

        if (target != null) JsonConvert.PopulateObject(jsonString, target);
    }
    
    public static void Save<T>(T data, string filePath)
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        var jsonString = JsonConvert.SerializeObject(data, Formatting.Indented, settings);

        var tmp = filePath + ".tmp";
        File.WriteAllText(tmp, jsonString);
        
        if (File.Exists(filePath)) File.Delete(filePath);

        File.Move(tmp, filePath);
    }
}