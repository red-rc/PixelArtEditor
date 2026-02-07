using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace PixelArtEditor.ViewModels;

public class ExportParams : IPreviewParams
{
    public short Width { get; set; }
    public short Height { get; set; }
    public Vector SelectedDPI { get; set; } = new Vector(96, 96);
}

public class ExportDialogVM : ReactiveObject
{
    private ExportParams _livePreviewParams = new();
    public ExportParams LivePreviewParams
    {
        get => _livePreviewParams;
        set => this.RaiseAndSetIfChanged(ref _livePreviewParams, value);
    }

    private readonly WriteableBitmap? PreviewBitmap = Services.ExportPreview.PreviewBitmap;

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

    public ImagePropertiesUCVM ImageProperties { get; }

    public ExportDialogVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        if (PreviewBitmap != null)
        {
            ImageProperties.SelectedWidth = (short)PreviewBitmap.Size.Width;
            ImageProperties.SelectedHeight = (short)PreviewBitmap.Size.Height;
            ImageProperties.SelectedDPI = PreviewBitmap.Dpi;
        }

        ConfirmCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await ImageExportService.ExportImageAsync(dialog, LivePreviewParams);
            dialog.Close();
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);
        SetupReactiveLivePreview();
    }

    private void SetupReactiveLivePreview()
    {
        var propsObservable = ImageProperties.WhenAnyValue(
            ip => ip.SelectedWidth,
            ip => ip.SelectedHeight,
            ip => ip.SelectedDPI);

        propsObservable.Subscribe(tuple =>
        {
            LivePreviewParams = new ExportParams
            {
                Width = tuple.Item1,
                Height = tuple.Item2,
                SelectedDPI = tuple.Item3
            };
        });
    }
}