using System.IO;

namespace PixelArtEditor.AppServices.Serialization;

public static class JsonService
{
    public static T? Load<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;
            
        var jsonString = File.ReadAllText(filePath);
        return string.IsNullOrWhiteSpace(jsonString) ? default : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonString);
    }
    
    public static void Populate<T>(T target, string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"{LocalizationService.Get("FileNotFound")}: {filePath}");

        var jsonString = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(jsonString)) throw new InvalidDataException($"{LocalizationService.Get("EmptyConfig")}");

        if (target != null) Newtonsoft.Json.JsonConvert.PopulateObject(jsonString, target);
    }
    
    public static void Save<T>(T data, string filePath)
    {
        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

        var tmp = filePath + ".tmp";
        File.WriteAllText(tmp, jsonString);
        
        if (File.Exists(filePath)) File.Delete(filePath);

        File.Move(tmp, filePath);
    }
}