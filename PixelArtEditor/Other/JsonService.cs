using System.IO;
using System.Text.Json;

namespace PixelArtEditor.Other;

public static class JsonService
{
    public static T? Load<T>(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");
            
        var jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(jsonString);
    }
    
    public static void Save<T>(T data, string filePath)
    {
        var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
            
        File.WriteAllText(filePath, jsonString);
    }
}