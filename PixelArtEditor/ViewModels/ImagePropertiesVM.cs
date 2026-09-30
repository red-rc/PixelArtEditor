using Avalonia.Controls;
using PixelArtEditor.AppServices.Bitmap;
using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.Models.Canvas;
using System;

namespace PixelArtEditor.ViewModels;

public enum PreviewTrigger
{
    Size,
    Palette,
    Other
}

public class ImagePropertiesVM : ReactiveObject
{
    public ImagePropertiesUCVM ImageProps { get; }

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> CancelCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCommand { get; }

    public ImagePropertiesVM(Window dialog, PixelModel model, EditorVM editorVM, bool export = false)
    {
        ImageProps = new ImagePropertiesUCVM();

        ImageProps.LoadFrom(model);

        ImageProps.WhenAnyValue(x => x.Width, x => x.Height).Subscribe(_ => UpdatePreview(model, PreviewTrigger.Size));
        ImageProps.WhenAnyValue(x => x.ColorModeName, x => x.BitDepthName)
            .Subscribe(_ => UpdatePreview(model, PreviewTrigger.Other));
        ImageProps.WhenAnyValue(x => x.Model.Palette).Subscribe(_ => UpdatePreview(model, PreviewTrigger.Palette));

        ResetCommand = ReactiveCommand.Create(() => {
            ImageProps.LoadFrom(model);
            ImageProps.RenderBitmap = BitmapService.CreateBitmap(model.Data, model.Width, model.Height);
        });

        CancelCommand = ReactiveCommand.Create(() =>
        {
            ImageProps.LoadFrom(model);
            dialog.Close();
        });

        SaveCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            ImageProps.SaveTo(editorVM, model);

            if (!export)
            {
                var mode = editorVM.Model.ColorMode;
                var layers = editorVM.LayerManager;

                if (mode == ColorMode.Indexed && editorVM.Model.Palette is not null)
                    layers.ToIndexed(editorVM.Model.Palette);
                else if (mode == ColorMode.Grayscale)
                    layers.ToGrayscale();
                else if (mode == ColorMode.GrayscaleAlpha)
                    layers.ToGrayscaleAlpha();
                else if (mode is ColorMode.RGB or ColorMode.BGR)
                    layers.ToRgb();
                else if (mode == ColorMode.RG)
                    layers.ToRedGreen();
                else if (mode == ColorMode.A)
                    layers.ToAlpha();
            }
            else
            {
                await ImageExportService.ExportImageAsync(dialog, 
                    ImageProps.GetFinalPixelModel(model.Data, model.Name, model.Extension, model.Palette, model.DicomDataset));
            }

            dialog.Close();
        });
    }

    private byte[] ResizeData(byte[] data, int width, int height)
    {
        var result = BitmapService.ResizePixelData(data, width, height, ImageProps.Width, ImageProps.Height);

        ImageProps.Model.Width = ImageProps.Width;
        ImageProps.Model.Height = ImageProps.Height;

        return result;
    }

    private byte[] ConvertData(PixelModel model, PreviewTrigger trigger)
    {
        byte[] result;

        if (ImageProps.ColorMode == model.ColorMode) return model.Data;

        if (ImageProps.ColorMode == ColorMode.Indexed)
        {
            if (trigger == PreviewTrigger.Palette)
                ImageProps.isPaletteSetByUser = true;

            var srcPalette = ImageProps.isPaletteSetByUser ? ImageProps.Model.Palette : null;

            var (quantized, palette) = BitmapService.GetQuantized(model.Data, ImageProps.BitDepth, srcPalette);

            if (palette != srcPalette)
                ImageProps.isPaletteSetByUser = false;

            result = quantized;
            model.Palette = palette;
            model.BitDepth = ImageProps.BitDepth;
        }
        else
        {
            result = ImageProps.ColorMode switch
            {
                ColorMode.Grayscale => ImageConverterService.ToGrayscale(ImageConverterService.ToRgb(model.Data)),
                ColorMode.GrayscaleAlpha => ImageConverterService.ToGrayscale(model.Data),
                ColorMode.RGB or ColorMode.BGR => ImageConverterService.ToRgb(model.Data),
                ColorMode.RG => ImageConverterService.ToRedGreen(model.Data),
                ColorMode.A => ImageConverterService.ToAlpha(model.Data),
                _ => model.Data
            };
        }

        ImageProps.Model.BitDepth = ImageProps.BitDepth;

        return result;
    }

    private void UpdatePreview(PixelModel model, PreviewTrigger trigger)
    {
        byte[]? previewData;

        if (trigger != PreviewTrigger.Size)
        {
            previewData = ConvertData(model, trigger);

            if (ImageProps.Width != model.Width || ImageProps.Height != model.Height)
                previewData = ResizeData(previewData, model.Width, model.Height);
        }
        else
            previewData = ResizeData(ImageProps.Model.Data, model.Width, model.Height);
        
        ImageProps.RenderBitmap?.Dispose();
        ImageProps.RenderBitmap = BitmapService.CreateBitmap(previewData, ImageProps.Width, ImageProps.Height);

        ImageProps.PushRenderData();
    }
}