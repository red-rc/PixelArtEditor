using Avalonia.Controls;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using ReactiveUI;
using System;
using System.Reactive;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProperties { get; }
    public PixelModel LivePreviewParams => ImageProperties.LivePreviewParams;

    private readonly PixelModel? _originalModel = Services.ImageData.Model;

    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public ImagePropertiesVM(Window dialog)
    {
        ImageProperties = new ImagePropertiesUCVM();

        ImageProperties.WhenAnyValue(x => x.LivePreviewParams)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(LivePreviewParams)));

        // заповнюємо з поточної моделі
        if (_originalModel is not null)
        {
            ImageProperties.Width = _originalModel.Width;
            ImageProperties.Height = _originalModel.Height;
            ImageProperties.ColorMode = _originalModel.Mode;
            ImageProperties.BitDepth = _originalModel.BitDepth;
            ImageProperties.ColorSpace = _originalModel.ColorSpace;
            ImageProperties.AlphaFormat = _originalModel.Alpha;
            ImageProperties.DpiX = _originalModel.DpiX;
            ImageProperties.DpiY = _originalModel.DpiY;
            ImageProperties.BigEndian = _originalModel.BigEndian;
        }

        ResetCommand = ReactiveCommand.Create(ResetToOriginal);

        CancelCommand = ReactiveCommand.Create(() =>
        {
            ResetToOriginal();
            dialog.Close();
        });

        SaveCommand = ReactiveCommand.Create(() =>
        {
            if (_originalModel is null) return;

            var newWidth = ImageProperties.Width;
            var newHeight = ImageProperties.Height;

            // якщо розмір змінився — ресайзимо пікселі
            if ((newWidth != _originalModel.Width || newHeight != _originalModel.Height) && Services.ImageData.BitmapPixelData is not null)
            {
                Services.ImageData.BitmapPixelData = BitmapService.ResizePixelData(
                    Services.ImageData.BitmapPixelData,
                    _originalModel.Width, _originalModel.Height,
                    newWidth, newHeight);
                _originalModel.Width = newWidth;
                _originalModel.Height = newHeight;
                Services.ImageData.NotifyPixelDataChanged();
            }

            _originalModel.Mode = ImageProperties.ColorMode;
            _originalModel.BitDepth = ImageProperties.BitDepth;
            _originalModel.ColorSpace = ImageProperties.ColorSpace;
            _originalModel.Alpha = ImageProperties.AlphaFormat;
            _originalModel.DpiX = ImageProperties.DpiX;
            _originalModel.DpiY = ImageProperties.DpiY;
            _originalModel.BigEndian = ImageProperties.BigEndian;

            dialog.Close();
        });
    }

    private void ResetToOriginal()
    {
        if (_originalModel is null) return;

        ImageProperties.Width = _originalModel.Width;
        ImageProperties.Height = _originalModel.Height;
        ImageProperties.ColorMode = _originalModel.Mode;
        ImageProperties.BitDepth = _originalModel.BitDepth;
        ImageProperties.ColorSpace = _originalModel.ColorSpace;
        ImageProperties.AlphaFormat = _originalModel.Alpha;
        ImageProperties.DpiX = _originalModel.DpiX;
        ImageProperties.DpiY = _originalModel.DpiY;
        ImageProperties.BigEndian = _originalModel.BigEndian;
    }
}