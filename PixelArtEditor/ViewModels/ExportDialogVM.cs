using Avalonia.Controls;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace PixelArtEditor.ViewModels;

public class ExportDialogVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProperties { get; }
    public PixelModel LivePreviewParams => ImageProperties.LivePreviewParams;

    private LayerModel _layer = null!;
    public LayerModel Layer
    {
        get => _layer;
        set
        {
            if (_layer == value) return;
            
            _layer = value;
            this.RaisePropertyChanged(nameof(Layer));
        }
    }

    private readonly PixelModel _model = null!;

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }


    public ExportDialogVM(Window dialog, PixelModel model)
    {
        _model = model;
        ImageProperties = new ImagePropertiesUCVM();

        ImageProperties.WhenAnyValue(
                x => x.Width,
                x => x.Height,
                x => x.PixelData
            )
            .Subscribe(_ =>
            {
                var preview = ImageProperties.LivePreviewParams;

                if (preview?.Data == null || preview.Data.Length == 0) return;

                Layer = new LayerModel(
                    preview.Width,
                    preview.Height,
                    preview.Data,
                    "Preview Layer");
            });

        ImageProperties.LoadFrom(_model);

        ConfirmCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await ImageExportService.ExportImageAsync(dialog, LivePreviewParams);
            dialog.Close();
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);
    }
}