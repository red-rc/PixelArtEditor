using Avalonia.Controls;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System;
using System.Reactive;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProperties { get; }
    public PixelModel LivePreviewParams => ImageProperties.LivePreviewParams;
    public LayerModel Layer => ImageProperties.Layer;
    private readonly PixelModel _model = null!;  
    
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public ImagePropertiesVM(Window dialog, PixelModel model)
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

        ResetCommand = ReactiveCommand.Create(() => ImageProperties.LoadFrom(_model));

        CancelCommand = ReactiveCommand.Create(() =>
        {
            ImageProperties.LoadFrom(_model);
            dialog.Close();
        });

        SaveCommand = ReactiveCommand.Create(() =>
        {
            var newWidth = ImageProperties.Width;
            var newHeight = ImageProperties.Height;

            // якщо розмір змінився — ресайзимо пікселі
            if ((newWidth != _model.Width || newHeight != _model.Height) && _model.Data is not null)
            {
                _model.Data = BitmapService.ResizePixelData(
                    _model.Data,
                    _model.Width, _model.Height,
                    newWidth, newHeight);
                _model.Width = newWidth;
                _model.Height = newHeight;
                Services.ModelData.NotifyModelChanged();
            }

            ImageProperties.SaveTo(_model);

            dialog.Close();
        });
    }
}