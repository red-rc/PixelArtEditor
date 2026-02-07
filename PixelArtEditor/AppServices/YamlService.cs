using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PixelArtEditor.AppServices;

public static class YamlService
{
    public static Dictionary<string, string> Load(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var result = deserializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        return result;
    }

    public static void Save(Dictionary<string, string> data, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(data);
        File.WriteAllText(path, yaml);
    }
}
