using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace PixelArtEditor.ViewModels;

public class PropertiesParams : IPreviewParams
{
    public short Width { get; set; }
    public short Height { get; set; }
}

public class ImagePropertiesVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProperties { get; }

    private PropertiesParams _livePreviewParams = new();
    public PropertiesParams LivePreviewParams
    {
        get => _livePreviewParams;
        set => this.RaiseAndSetIfChanged(ref _livePreviewParams, value);
    }
    private readonly WriteableBitmap? PreviewBitmap = Services.ExportPreview.PreviewBitmap;
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public ImagePropertiesVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        if (PreviewBitmap != null)
        {
            ImageProperties.SelectedWidth = (short)PreviewBitmap.Size.Width;
            ImageProperties.SelectedHeight = (short)PreviewBitmap.Size.Height;
        }

        ResetCommand = ReactiveCommand.Create(OnClosing);
        CancelCommand = ReactiveCommand.Create(() =>
        {
            OnClosing();
            dialog.Close();
        });
        SaveCommand = ReactiveCommand.Create(() =>
        {
            if (PreviewBitmap == null) return;

            var bitmap = PreviewBitmap;
            BitmapService.UpdateBitmapProperties(ref bitmap, LivePreviewParams.Width, LivePreviewParams.Height, new Vector(96, 96), AlphaFormat.Unpremul);

            Services.ExportPreview.PreviewBitmap = bitmap;
            Services.RenderInvalidation.BitmapDirty = true;
            dialog.Close();
        });

        SetupReactiveLivePreview();
    }

    private void SetupReactiveLivePreview()
    {
        this.WhenAnyValue(
            x => x.ImageProperties.SelectedWidth,
            x => x.ImageProperties.SelectedHeight
        ).Subscribe(tuple =>
        {
            LivePreviewParams = new PropertiesParams
            {
                Width = tuple.Item1,
                Height = tuple.Item2
            };
        });
    }

    public void OnClosing()
    {
        if (PreviewBitmap != null)
        {
            ImageProperties.SelectedWidth = (short)PreviewBitmap.Size.Width;
            ImageProperties.SelectedHeight = (short)PreviewBitmap.Size.Height;
        }

        foreach (var prop in typeof(ISettingsService).GetProperties())
        {
            this.RaisePropertyChanged(prop.Name);
        }
    }
}