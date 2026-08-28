using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PixelArtEditor.Models;

public sealed class PreviewData(int width, int height, WriteableBitmap? bitmap, Color? color) : INotifyPropertyChanged
{
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
    public WriteableBitmap? Bitmap { get; set; } = bitmap;
    public Color? Color { get; set; } = color;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public void NotifyPropertyChanged() => OnPropertyChanged(null);
}
