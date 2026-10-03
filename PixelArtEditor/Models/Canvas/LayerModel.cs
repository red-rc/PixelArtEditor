using Avalonia;
using PixelArtEditor.AppServices.Bitmap;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PixelArtEditor.Models.Canvas;

public class LayerModel(int width, int height, byte[] pixelData, string name, bool isEmpty = false) : INotifyPropertyChanged
{
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;

    private byte[] _Data = pixelData;
    public byte[] Data
    {
        get => _Data;
        set
        {
            _Data = value;
            OnPropertyChanged();
        }
    }

    private TiledBitmap? _tiles;
    public TiledBitmap Tiles
    {
        get
        {
            if (_tiles is null)
            {
                _tiles = new TiledBitmap(Width, Height);
                _tiles.Update(Data, new PixelRect(0, 0, Width, Height), IsEmpty);
            }

            IsEmpty = false;
            return _tiles;
        }
        set => _tiles = value;
    }

    public string Name { get; set; } = name;
    public PixelRect? ThumbDirtyRect { get; set; }
    public bool IsEmpty = isEmpty;

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

    public void NotifyPixelDataChanged() => OnPropertyChanged(nameof(Data));
}