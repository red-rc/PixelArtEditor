using PixelArtEditor.Styles;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace PixelArtEditor.AppServices.Serialization;

public static class JsonService
{
    public static T? Load<T>(string filePath, JsonTypeInfo<T> typeInfo)
    {
        if (!File.Exists(filePath)) return default;

        var jsonString = File.ReadAllText(filePath);
        return string.IsNullOrWhiteSpace(jsonString) ? default : JsonSerializer.Deserialize(jsonString, typeInfo);
    }

    public static void Save<T>(T data, string filePath, JsonTypeInfo<T> typeInfo)
    {
        var jsonString = JsonSerializer.Serialize(data, typeInfo);

        var tmp = filePath + ".tmp";
        File.WriteAllText(tmp, jsonString);

        if (File.Exists(filePath)) File.Delete(filePath);
        File.Move(tmp, filePath);
    }
}

[JsonSerializable(typeof(SettingsData))]
[JsonSerializable(typeof(ThemeData[]))]
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = false)]
public partial class AppJsonContext : JsonSerializerContext;