using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;

namespace PixelArtEditor.AppServices.ImageProcessing.Formats;

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

    // Зберігає одне зображення у форматі .ico (PNG-фрейм, Vista-style)
    public static void Save(Stream stream, Image<Rgba32> image)
    {
        if (image.Width > 256 || image.Height > 256)
            throw new InvalidOperationException(LocalizationService.Get("IcoTooLarge"));

        using var pngStream = new MemoryStream();
        image.Save(pngStream, new PngEncoder());
        var pngBytes = pngStream.ToArray();

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // ICONDIR
        writer.Write((ushort)0); // reserved
        writer.Write((ushort)1); // type = icon
        writer.Write((ushort)1); // count

        // ICONDIRENTRY
        writer.Write((byte)(image.Width >= 256 ? 0 : image.Width));
        writer.Write((byte)(image.Height >= 256 ? 0 : image.Height));
        writer.Write((byte)0);  // color count
        writer.Write((byte)0);  // reserved
        writer.Write((ushort)1);  // planes
        writer.Write((ushort)32); // bpp
        writer.Write((uint)pngBytes.Length);
        writer.Write((uint)22);   // offset = 6 (header) + 16 (entry)

        writer.Write(pngBytes);
    }
}