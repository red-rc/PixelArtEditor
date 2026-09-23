using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.AppServices;
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

        ImageProps.WhenAnyValue(x => x.Width, x => x.Height).Subscribe(_ => UpdatePreview(model));
        ImageProps.WhenAnyValue(x => x.ColorMode).Subscribe(_ => UpdatePreview(model));
        ImageProps.WhenAnyValue(x => x.BitDepth).Subscribe(_ => UpdatePreview(model));

        ResetCommand = ReactiveCommand.Create(() => {
            ImageProps.LoadFrom(model, handleModelChanged);
            ImageProps.RenderBitmap = BitmapService.CreateBitmap(model.Width, model.Height, model.Data);
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
                    var (indices, palette) = ImageConverterService.ToIndexed(
                        ImageProps.Model.Data,
                        model.Width,
                        ImageProps.BitDepth,
                        model.Palette?.Colors.Count,
                        model.Palette?.QuantizationMethod,
                        model.Palette?.Dither);

                    var tempModel = new PixelModel
                    {
                        Width = model.Width,
                        Height = model.Height,
                        Mode = model.Mode,
                        BitDepth = model.BitDepth,
                        Data = indices,
                        Palette = palette
                    };

                    previewData = BitmapService.SwapRB(PixelModelService.ToRgba32(tempModel));
                    model.Palette = palette;
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
                ImageProps.RenderBitmap = BitmapService.CreateBitmap(ImageProps.Width, ImageProps.Height, previewData);
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
            ImageProps.RenderBitmap = BitmapService.CreateBitmap(ImageProps.Width, ImageProps.Height, previewData);
        }

        ImageProps.PushRenderData();
    }
}