using PixelArtEditor.Other;
using System;

namespace PixelArtEditor.AppServices;

public class ImageDataService
{
    public event Action? PixelDataChanged;
    public void NotifyPixelDataChanged() => PixelDataChanged?.Invoke();
    public byte[]? BitmapPixelData { get; set; }

    private PixelModel? _model;

    public event Action<PixelModel?>? ModelChanged;

    public PixelModel? Model
    {
        get => _model;
        set
        {
            _model = value;
            ModelChanged?.Invoke(value);
        }
    }
}
