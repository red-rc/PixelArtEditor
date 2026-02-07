using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices;

public class ExportPreviewService
{
    public WriteableBitmap? PreviewBitmap { get; set; } = null!;

    public WriteableBitmap? GetUpdatedPreview(ExportParams newParams)
    {
        if (PreviewBitmap is null) return null;
        var bitmap = PreviewBitmap;
        BitmapService.UpdateBitmapProperties(ref bitmap,
            newParams.Width, 
            newParams.Height,
            newParams.SelectedDPI,
            AlphaFormat.Unpremul);
        return bitmap;
    }
}
