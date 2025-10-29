using ReactiveUI;
using System.Collections.Generic;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesViewModel : ReactiveObject
{
    private int _selectedDpi = 96;
    public int SelectedDPI
    {
        get => _selectedDpi;
        set => this.RaiseAndSetIfChanged(ref _selectedDpi, value);
    }

    public List<string> PixelFormatOptions { get; set; } = ["Bgra8888"];

    private string _selectedPixelFormat = "Bgra8888";
    public string SelectedPixelFormat
    {
        get => _selectedPixelFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedPixelFormat, value);
    }

    public List<string> AlphaFormatOptions { get; set; } = ["Unpremul"];

    private string _selectedAlphaFormat = "Unpremul";
    public string SelectedAlphaFormat
    {
        get => _selectedAlphaFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedAlphaFormat, value);
    }
}
