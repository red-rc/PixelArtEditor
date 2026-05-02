using Avalonia.Controls;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace PixelArtEditor.ViewModels;

public class ExportDialogVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProperties { get; }
    public PixelModel LivePreviewParams => ImageProperties.LivePreviewParams;

    private readonly PixelModel? _originalModel = Services.ImageData.Model;

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }


    public ExportDialogVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        ImageProperties.WhenAnyValue(x => x.LivePreviewParams)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(LivePreviewParams)));

        if (_originalModel is not null) 
            ImageProperties.LoadFrom(_originalModel);

        ConfirmCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await ImageExportService.ExportImageAsync(dialog, LivePreviewParams);
            dialog.Close();
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);
    }
}