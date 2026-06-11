using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices.Canvas;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PixelArtEditor.Models.Canvas;

public class LayerModel(int width, int height, byte[] pixelData, string name) : INotifyPropertyChanged
{
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
    public byte[] PixelData { get; set; } = pixelData;
    public WriteableBitmap RenderBitmap { get; set; } = BitmapService.CreateBitmap(width, height, pixelData);
    public WriteableBitmap? PreviewBitmap { get; set; }

    public string Name { get; set; } = name;

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(); }
    }

    private float _opacity = 1.0f;
    public float Opacity
    {
        get => _opacity;
        set { _opacity = value; OnPropertyChanged(); }
    }

    public bool IsLocked { get; set; } = false;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}