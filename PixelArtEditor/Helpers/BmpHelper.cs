using Avalonia.Media;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PixelArtEditor.Helpers;

public class BmpHelper
{
    public static List<Color> ExtractBmpPalette(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // 1. Перевірка сигнатури 'BM' (BITMAPFILEHEADER)
        if (stream.Length < 14 || reader.ReadByte() != 'B' || reader.ReadByte() != 'M') return [];

        // 2. Читаємо розмір DIB-заголовка (починається з 14-го байта)
        stream.Position = 14;
        var dibHeaderSize = reader.ReadInt32();

        // 3. Читаємо BitsPerPixel (зміщення 14 + 14 = 28)
        stream.Position = (dibHeaderSize == 12) ? 24 : 28;
        var bitsPerPixel = reader.ReadInt16();

        // Палітра існує тільки для індексованих BMP (<= 8 біт: 1, 4 або 8 bpp)
        if (bitsPerPixel > 8) return [];

        // 4. Читаємо кількість кольорів у палітрі (biClrUsed, зміщення 14 + 32 = 46)
        var colorsCount = 0;
        if (dibHeaderSize >= 40) // Для BITMAPINFOHEADER і новіших
        {
            stream.Position = 46;
            colorsCount = reader.ReadInt32();
        }

        // Якщо biClrUsed == 0, використовується максимально можлива кількість для даного bpp (2^bpp)
        if (colorsCount == 0)
            colorsCount = 1 << bitsPerPixel; // 2, 16 або 256

        // 5. Таблиця кольорів починається одразу після DIB-заголовка
        stream.Position = 14 + dibHeaderSize;

        var palette = new List<Color>(colorsCount);

        // Старі заголовок OS/2 (12 байт) використовують RGBTRIPLE (3 байти), Windows — RGBQUAD (4 байти)
        var bytesPerEntry = (dibHeaderSize == 12) ? 3 : 4;

        for (var i = 0; i < colorsCount; i++)
        {
            byte b = reader.ReadByte();
            byte g = reader.ReadByte();
            byte r = reader.ReadByte();

            if (bytesPerEntry == 4)
                reader.ReadByte(); // Пропускаємо падінг (Reserved/Alpha)

            palette.Add(Color.FromArgb(255, r, g, b));
        }

        return palette;
    }
}