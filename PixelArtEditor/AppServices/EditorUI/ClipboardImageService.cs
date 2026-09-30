using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.Models.Canvas;
using System.IO;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.EditorUI;

public class ClipboardImageService
{
    public static async Task<PixelModel?> GetModel(IClipboard? clipboard)
    {
        if (clipboard is null) return null;

        using var data = await clipboard.TryGetDataAsync();
        if (data is null) return null;

        var file = await data.TryGetFileAsync();

        if (file is IStorageFile f)
            return await ImageImportService.GetPixelModelFromFile(f, false);

        using var bitmap = await data.TryGetBitmapAsync();
        if (bitmap is null) return null;

        using var ms = new MemoryStream();

        bitmap.Save(ms, new PngBitmapEncoderOptions());
        ms.Position = 0;

        return await ImageImportService.GetPixelModelFromStream(ms, showError: false);
    }
}