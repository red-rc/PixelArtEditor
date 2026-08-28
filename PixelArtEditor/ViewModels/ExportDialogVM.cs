using Avalonia.Controls;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;

namespace PixelArtEditor.ViewModels;

public class ExportDialogVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProperties { get; }

    private readonly PixelModel _model;

    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ConfirmCommand { get; }


    public ExportDialogVM(Window dialog, PixelModel model)
    {
        _model = model;

        ImageProperties = new ImagePropertiesUCVM();

        ImageProperties.LoadFrom(_model);
        ImageProperties.RenderBitmap = BitmapService.CreateBitmap(_model.Width, _model.Height, _model.Data);
        ImageProperties.PushRenderData();

        ConfirmCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await ImageExportService.ExportImageAsync(dialog, ImageProperties.GetFinalPixelMode(_model.Data));
            dialog.Close();
        });

        CancelCommand = ReactiveCommand.Create(dialog.Close);
    }
}