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

    public LayerModel Layer => ImageProperties.Layer;

    private readonly PixelModel _model = null!;

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }


    public ExportDialogVM(Window dialog, PixelModel model)
    {
        _model = model;
        ImageProperties = new ImagePropertiesUCVM();

        ImageProperties.WhenAnyValue(x => x.LivePreviewParams)
            .Subscribe(_ =>
            {
                ImageProperties.LivePreviewParams.Data = _model.Data;
                this.RaisePropertyChanged(nameof(LivePreviewParams));
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