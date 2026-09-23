using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System;

namespace PixelArtEditor.AppServices.ImageProcessing;

public static class ImagePropertiesMapper
{
    public static void LoadFrom(this ImagePropertiesUCVM vm, PixelModel model, Action onModelChanged)
    {
        vm.Model?.ModelChanged -= onModelChanged;

        vm.Model = new PixelModel 
        { 
            Width = model.Width,
            Height = model.Height,
            Data = model.Data,
            Palette = null,
            BitDepth = model.BitDepth
        };

        vm.Model.ModelChanged += onModelChanged;

        vm.Width = model.Width;
        vm.Height = model.Height;
        vm.ColorModeName = model.Mode.ToString();
        vm.BitDepthName = model.BitDepth.ToString();
        vm.ColorSpaceName = model.ColorSpace.ToString();
        vm.AlphaFormatName = model.Alpha.ToString();
        vm.DpiX = model.DpiX;
        vm.DpiY = model.DpiY;
    }

    public static void SaveTo(this ImagePropertiesUCVM vm, EditorVM editorVM, PixelModel model)
    {
        editorVM.Model.Mode = vm.ColorMode;
        editorVM.Model.BitDepth = vm.BitDepth;
        if (editorVM.Model.Mode == ColorMode.Indexed) editorVM.Model.Palette = model.Palette;
        editorVM.Model.ColorSpace = vm.ColorSpace;
        editorVM.Model.Alpha = vm.AlphaFormat;
        editorVM.Model.DpiX = vm.DpiX;
        editorVM.Model.DpiY = vm.DpiY;
        editorVM.Model.Width = vm.Width;
        editorVM.Model.Height = vm.Height;
        editorVM.Model.NotifyModelChanged();
    }
}
