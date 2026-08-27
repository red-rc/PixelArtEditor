using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProperties { get; }

    private readonly PixelModel _model;  
    
    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; } 

    public ImagePropertiesVM(Window dialog, PixelModel model)
    {
        _model = model;
        ImageProperties = new ImagePropertiesUCVM();

        ImageProperties.LoadFrom(_model);

        ImageProperties.WhenAnyValue(x => x.Width, x => x.Height).Subscribe(_ =>
        {
            if (ImageProperties.PixelData is null || ImageProperties.PixelData.Length == 0) return;

            ImageProperties.PixelData = BitmapService.ResizePixelData(
                _model.Data, _model.Width, _model.Height, ImageProperties.Width, ImageProperties.Height);

            ImageProperties.PushRenderData();
        });

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

            if ((newWidth != _model.Width || newHeight != _model.Height) && _model.Data is not null)
            {
                _model.Data = BitmapService.SwapRB(BitmapService.ResizePixelData(
                    _model.Data,
                    _model.Width, _model.Height,
                    newWidth, newHeight));
                _model.Width = newWidth;
                _model.Height = newHeight;
                _model.NotifyModelChanged();
            }

            ImageProperties.SaveTo(_model);

            dialog.Close();
        });
    }
}