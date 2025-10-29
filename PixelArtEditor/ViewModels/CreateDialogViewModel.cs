using System;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;

namespace PixelArtEditor.ViewModels;

public class CreateParams
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Color BackgroundColor { get; set; } = Colors.White;
}

public class CreateDialogViewModel : ReactiveObject
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
    
    private Color _selectedColor = Colors.White;
    public Color SelectedColor
    {
        get => _selectedColor;
        set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
    }
    
    private CreateParams _livePreviewParams = new();
    public CreateParams LivePreviewParams
    {
        get => _livePreviewParams;
        set => this.RaiseAndSetIfChanged(ref _livePreviewParams, value);
    }

    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public CreateDialogViewModel(Window dialog)
    {
        this.WhenAnyValue(
            x => x.SelectedWidth,
            x => x.SelectedHeight,
            x => x.SelectedColor
        ).Subscribe(tuple =>
        {
            LivePreviewParams = new CreateParams
            {
                Width = tuple.Item1,
                Height = tuple.Item2,
                BackgroundColor = tuple.Item3
            };
        });
        
        CreateCommand = ReactiveCommand.Create(() =>
        {
            dialog.Close(new CreateParams
            {
                Width = SelectedWidth,
                Height = SelectedHeight,
                BackgroundColor = SelectedColor
            });
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);
    }
}