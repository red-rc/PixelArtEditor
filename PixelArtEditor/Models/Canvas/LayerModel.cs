using Avalonia;
using PixelArtEditor.AppServices.Bitmap;
using ReactiveUI.SourceGenerators;

namespace PixelArtEditor.Models.Canvas;

public partial class LayerModel(int width, int height, byte[] data, string name, bool isEmpty = false) : ReactiveObject
{
    [Reactive] public partial int Width { get; set; } = width;
    [Reactive] public partial int Height { get; set; } = height;
    [Reactive] public partial byte[] Data { get; set; } = data;

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

    [Reactive] public partial string Name { get; set; } = name;
    [Reactive] public partial PixelRect? ThumbDirtyRect { get; set; }
    [Reactive] public partial bool IsEmpty { get; set; } = isEmpty;
    [Reactive] public partial bool IsVisible { get; set; } = true;
    [Reactive] public partial float Opacity { get; set; } = 1.0f;
    [Reactive] public partial bool IsLocked { get; set; } = false;

    public void Resize(int newWidth, int newHeight, byte[] newData)
    {
        Width = newWidth;
        Height = newHeight;
        Data = newData;
        ThumbDirtyRect = null;

        _tiles?.Dispose();
        _tiles = new TiledBitmap(newWidth, newHeight);

        this.RaisePropertyChanged(nameof(Width));
        this.RaisePropertyChanged(nameof(Height));
        this.RaisePropertyChanged(nameof(Tiles));
        this.RaisePropertyChanged(nameof(Data));
    }
}