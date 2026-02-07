using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.Other;
using ReactiveUI;
using System;
using System.Reactive;

namespace PixelArtEditor.ViewModels;

public class CreateParams : IPreviewParams
{
    public short Width { get; set; }
    public short Height { get; set; }
    public Color BackgroundColor { get; set; } = Colors.White;
}

public class CreateDialogVM : ReactiveObject
{    
    private Color _selectedColor = Colors.White;
    public Color SelectedColor
    {
        get => _selectedColor;
        set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
    }

    public ImagePropertiesUCVM ImageProperties { get; }

    private CreateParams _livePreviewParams = new();
    public CreateParams LivePreviewParams
    {
        get => _livePreviewParams;
        set => this.RaiseAndSetIfChanged(ref _livePreviewParams, value);
    }

    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public CreateDialogVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();
        
        CreateCommand = ReactiveCommand.Create(() =>
        {
            dialog.Close(new CreateParams
            {
                Width = ImageProperties.SelectedWidth,
                Height = ImageProperties.SelectedHeight,
                BackgroundColor = SelectedColor
            });
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);

        SetupReactiveLivePreview();
    }


    private void SetupReactiveLivePreview()
    {
        this.WhenAnyValue(
            x => x.ImageProperties.SelectedWidth,
            x => x.ImageProperties.SelectedHeight,
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
    }
}