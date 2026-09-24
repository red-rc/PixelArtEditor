using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProps { get; }

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public ImagePropertiesVM(Window dialog, PixelModel model, EditorVM editorVM, bool export = false)
    {
        ImageProps = new ImagePropertiesUCVM();

        void handleModelChanged()
        {
            model.Data = ImageProps.Model.Data;
            model.Palette = ImageProps.Model.Palette;
            UpdatePreview(model);
        }

        ImageProps.LoadFrom(model, handleModelChanged);

        ImageProps.WhenAnyValue(x => x.Width, x => x.Height, x => x.ColorMode, x => x.BitDepth)
            .Subscribe(_ => UpdatePreview(model));

        ResetCommand = ReactiveCommand.Create(() => {
            ImageProps.LoadFrom(model, handleModelChanged);
            ImageProps.RenderBitmap = BitmapService.CreateBitmap(model.Data, model.Width, model.Height);
        });

        CancelCommand = ReactiveCommand.Create(() =>
        {
            ImageProps.LoadFrom(model, handleModelChanged);
            dialog.Close();
        });

        SaveCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            ImageProps.SaveTo(editorVM, model);

            if (!export)
            {
                if (editorVM.Model.Mode == ColorMode.Indexed && editorVM.Model.Palette is not null)
                    editorVM.LayerManager.QuantizeToPalette(editorVM.Model.Palette);
            }
            else
            {
                await ImageExportService.ExportImageAsync(dialog, 
                    ImageProps.GetFinalPixelModel(model.Data, model.Name, model.Extension, model.Palette, model.DicomDataset));
            }

            dialog.Close();
        });
    }

    private void UpdatePreview(PixelModel model)
    {
        if (model.Data is null || model.Data.Length == 0) return;

        byte[]? previewData;

        if (ImageProps.Width == model.Width && ImageProps.Height == model.Height)
        {
            if (ImageProps.ColorMode == ColorMode.Indexed)
            {
                if (model.BitDepth == ImageProps.BitDepth && model.Palette is not null)
                    previewData = model.Data;
                else
                {
                    var (indices, palette) = 
                        BitmapService.GetQuantized(ImageProps.Model.Data, ImageProps.BitDepth, model.Palette);

                    previewData = indices;
                    model.Palette = palette;
                    model.BitDepth = ImageProps.BitDepth;
                }
            }
            else if (ImageProps.ColorMode == ColorMode.Grayscale)
                previewData = ImageConverterService.ConvertToGrayscale(ImageProps.Model.Data);
            else if (ImageProps.ColorMode == ColorMode.RGB)
                previewData = ImageConverterService.StripAlpha(ImageProps.Model.Data);
            else
                previewData = ImageProps.Model.Data;

            if (ImageProps.RenderBitmap is not null)
            {
                BitmapService.UpdateBitmap(ImageProps.RenderBitmap, previewData,
                    new Rect(0, 0, ImageProps.Width, ImageProps.Height));
            }
            else
            {
                ImageProps.RenderBitmap?.Dispose();
                ImageProps.RenderBitmap = BitmapService.CreateBitmap(previewData, ImageProps.Width, ImageProps.Height);
            }
        }
        else
        {
            previewData = BitmapService.ResizePixelData(
                ImageProps.Model.Data,
                model.Width,
                model.Height,
                ImageProps.Width, 
                ImageProps.Height);

            ImageProps.RenderBitmap?.Dispose();
            ImageProps.RenderBitmap = BitmapService.CreateBitmap(previewData, ImageProps.Width, ImageProps.Height);
        }

        ImageProps.PushRenderData();
    }
}