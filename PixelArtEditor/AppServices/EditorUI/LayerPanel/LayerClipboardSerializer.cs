using Avalonia.Input;
using PixelArtEditor.Models.Canvas;
using System.Collections.Generic;
using System.IO;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel;

public static class LayerClipboardSerializer
{
    public static readonly DataFormat<byte[]> FormatId = DataFormat.CreateBytesApplicationFormat("x-pixeller-layers");

    public static byte[] Serialize(List<LayerModel> layers)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(layers.Count);

        foreach (var l in layers)
        {
            w.Write(l.Width);
            w.Write(l.Height);
            w.Write(l.Name);
            w.Write(l.Opacity);
            w.Write(l.IsVisible);
            w.Write(l.IsLocked);
            w.Write(l.IsEmpty);
            w.Write(l.Data.Length);
            w.Write(l.Data);
        }

        return ms.ToArray();
    }

    public static List<LayerModel>? Deserialize(byte[] data)
    {
        try
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);

            var count = r.ReadInt32();
            var result = new List<LayerModel>(count);

            for (var i = 0; i < count; i++)
            {
                var width = r.ReadInt32();
                var height = r.ReadInt32();
                var name = r.ReadString();
                var opacity = r.ReadSingle();
                var isVisible = r.ReadBoolean();
                var isLocked = r.ReadBoolean();
                var isEmpty = r.ReadBoolean();
                var dataLen = r.ReadInt32();
                var pixelData = r.ReadBytes(dataLen);

                result.Add(new LayerModel(width, height, pixelData, name, isEmpty)
                {
                    Opacity = opacity,
                    IsVisible = isVisible,
                    IsLocked = isLocked
                });
            }

            return result;
        }
        catch { return null; }
    }
}