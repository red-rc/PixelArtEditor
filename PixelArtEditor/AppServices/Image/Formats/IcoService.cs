using System.IO;

namespace PixelArtEditor.AppServices.Image.Formats;

public static class IcoService
{
    // ICO: 6-байтовий header, потім N x 16-байтових ICONDIRENTRY, потім самі дані (PNG або BMP)
    public static byte[]? ExtractLargestImage(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        stream.Position = 0;

        var reserved = reader.ReadUInt16();
        var type = reader.ReadUInt16();
        var count = reader.ReadUInt16();

        if (reserved != 0 || type != 1 || count == 0) return null; // не .ico (type=2 — .cur)

        var bestSize = -1;
        var bestOffset = 0u;
        var bestLength = 0u;

        for (var i = 0; i < count; i++)
        {
            var width = reader.ReadByte();   // 0 означає 256
            var height = reader.ReadByte();  // 0 означає 256
            reader.ReadByte();  // color count
            reader.ReadByte();  // reserved
            reader.ReadUInt16(); // color planes
            reader.ReadUInt16(); // bits per pixel
            var bytesInRes = reader.ReadUInt32();
            var imageOffset = reader.ReadUInt32();

            var w = width == 0 ? 256 : width;
            var h = height == 0 ? 256 : height;
            var size = w * h;

            if (size <= bestSize) continue;

            bestSize = size;
            bestOffset = imageOffset;
            bestLength = bytesInRes;
        }

        if (bestSize < 0) return null;

        stream.Position = bestOffset;
        var data = new byte[bestLength];
        var read = 0;
        while (read < data.Length)
        {
            var n = stream.Read(data, read, data.Length - read);
            if (n == 0) break;
            read += n;
        }

        return data;
    }
}