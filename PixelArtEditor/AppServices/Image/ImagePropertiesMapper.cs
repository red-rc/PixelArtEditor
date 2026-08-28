using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.Image;

public static class ImagePropertiesMapper
{
    // Заповнює ImagePropertiesUCVM з PixelModel
    public static void LoadFrom(this ImagePropertiesUCVM vm, PixelModel model)
    {
        vm.Name = model.Name;
        vm.Extension = model.Extension;
        vm.Width = model.Width;
        vm.Height = model.Height;
        vm.ColorMode = model.Mode;
        vm.BitDepth = model.BitDepth;
        vm.ColorSpace = model.ColorSpace;
        vm.AlphaFormat = model.Alpha;
        vm.DpiX = model.DpiX;
        vm.DpiY = model.DpiY;
        vm.BigEndian = model.BigEndian;
    }

    // Зберігає значення з ImagePropertiesUCVM в PixelModel
    public static void SaveTo(this ImagePropertiesUCVM vm, PixelModel model)
    {
        model.Mode = vm.ColorMode;
        model.BitDepth = vm.BitDepth;
        model.ColorSpace = vm.ColorSpace;
        model.Alpha = vm.AlphaFormat;
        model.DpiX = vm.DpiX;
        model.DpiY = vm.DpiY;
        model.BigEndian = vm.BigEndian;
    }
}