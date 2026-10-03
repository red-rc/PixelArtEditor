using Avalonia;
using PixelArtEditor.AppServices.Bitmap;

namespace PixelArtEditor.Models.Canvas;

public class LayerModel(int width, int height, byte[] data, string name, bool isEmpty = false) : ReactiveObject
{
    private int _width = width;
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private int _height = height;
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    private byte[] _data = data;
    public byte[] Data
    {
        get => _data;
        set => this.RaiseAndSetIfChanged(ref _data, value);
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
        set => this.RaiseAndSetIfChanged(ref _tiles, value);
    }

    private string _name = name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private PixelRect? _thumbDirtyRect;
    public PixelRect? ThumbDirtyRect
    {
        get => _thumbDirtyRect;
        set => this.RaiseAndSetIfChanged(ref _thumbDirtyRect, value);
    }

    private bool _isEmpty = isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        set => this.RaiseAndSetIfChanged(ref _isEmpty, value);
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private float _opacity = 1.0f;
    public float Opacity
    {
        get => _opacity;
        set => this.RaiseAndSetIfChanged(ref _opacity, value);
    }

    private bool _isLocked = false;
    public bool IsLocked 
    {
        get => _isLocked;
        set => this.RaiseAndSetIfChanged(ref _isLocked, value);
    }

    public void Resize(int newWidth, int newHeight, byte[] newData)
    {
        _width = newWidth;
        _height = newHeight;
        _data = newData;
        _thumbDirtyRect = null;

        _tiles?.Dispose();
        _tiles = new TiledBitmap(newWidth, newHeight);

        this.RaisePropertyChanged(nameof(Width));
        this.RaisePropertyChanged(nameof(Height));
        this.RaisePropertyChanged(nameof(Tiles));
        this.RaisePropertyChanged(nameof(Data));
    }
}
