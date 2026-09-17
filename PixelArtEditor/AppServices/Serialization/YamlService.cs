using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace PixelArtEditor.AppServices.Serialization;

public class YamlData
{
    public Dictionary<string, string> LocalPairs { get; set; } = [];
}

public static class YamlService
{
    private static readonly IDeserializer Deserializer =
        new StaticDeserializerBuilder(new YamlStaticContext())
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

    public static YamlData Load(Stream stream)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        var text = reader.ReadToEnd().TrimStart('\uFEFF');
        return Deserializer.Deserialize<YamlData>(text);
    }
}

[YamlStaticContext]
[YamlSerializable(typeof(YamlData))]
public partial class YamlStaticContext : StaticContext;