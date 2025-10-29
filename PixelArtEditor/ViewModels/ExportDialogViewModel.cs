using Avalonia.Controls;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace PixelArtEditor.ViewModels;

public class ExportParams
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int SelectedDPI { get; set; }
    public string SelectedPixelFormat { get; set; } = "Bgra8888";
    public string SelectedAlphaFormat { get; set; } = "Unpremul";
    public string SelectedFormat { get; set; } = ".png";
}
public class ExportDialogViewModel : ReactiveObject
{
    private int _selectedWidth = 32;
    public int SelectedWidth
    {
        get => _selectedWidth;
        set => this.RaiseAndSetIfChanged(ref _selectedWidth, value);
    }

    private int _selectedHeight = 32;
    public int SelectedHeight
    {
        get => _selectedHeight;
        set => this.RaiseAndSetIfChanged(ref _selectedHeight, value);
    }
    public List<string> FormatOptions { get; set; } = [".png", ".jpg", ".bmp", ".ico"];

    private string _selectedFormat = ".png";
    public string SelectedFormat
    {
        get => _selectedFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedFormat, value);
    }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

    public ExportDialogViewModel(Window dialog)
    {
        ConfirmCommand = ReactiveCommand.Create(() =>
        {

        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);
    }
}
