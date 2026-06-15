using Avalonia.Controls;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesVM : ReactiveObject
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
    
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; } 

    public ImagePropertiesVM(Window dialog, PixelModel model)
    {
        _model = model;
        ImageProperties = new ImagePropertiesUCVM();

        ImageProperties.LoadFrom(_model);

        ImageProperties.WhenAnyValue(
                x => x.Width,
                x => x.Height
            )
            .Subscribe(_ =>
            {
                if (ImageProperties.LivePreviewParams?.Data == null || ImageProperties.LivePreviewParams.Data.Length == 0) return;

                ImageProperties.LivePreviewParams.Data = BitmapService.ResizePixelData(
                    ImageProperties.LivePreviewParams.Data,
                    _model.Width, _model.Height,
                    ImageProperties.LivePreviewParams.Width, ImageProperties.LivePreviewParams.Height);

                Layer = new LayerModel(
                    ImageProperties.LivePreviewParams.Width,
                    ImageProperties.LivePreviewParams.Height,
                    ImageProperties.LivePreviewParams.Data,
                    "Preview Layer");
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
                Services.ModelData.NotifyModelChanged();
            }

            ImageProperties.SaveTo(_model);

            dialog.Close();
        });
    }
}