using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices.Canvas;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PixelArtEditor.Models.Canvas;

public class LayerModel(int width, int height, byte[] pixelData, string name) : INotifyPropertyChanged
{
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;

    private byte[] _pixelData = pixelData;
    public byte[] PixelData
    {
        get => _pixelData;
        set
        {
            _pixelData = value;
            OnPropertyChanged();
        }
    }

    private WriteableBitmap? _renderBitmap;

    public WriteableBitmap RenderBitmap
    {
        get => _renderBitmap ??= BitmapService.CreateBitmap(Width, Height, PixelData);
        set => _renderBitmap = value;
    }

    public WriteableBitmap? PreviewBitmap { get; set; }

    public string Name { get; set; } = name;

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set 
        { 
            _isVisible = value; 
            OnPropertyChanged(); 
        }
    }

    private float _opacity = 1.0f;
    public float Opacity
    {
        get => _opacity;
        set 
        { 
            _opacity = value;
            OnPropertyChanged(); 
        }
    }

    public bool IsLocked { get; set; } = false;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void NotifyPixelDataChanged() => OnPropertyChanged(nameof(PixelData));
}