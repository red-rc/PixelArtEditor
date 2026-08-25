using Avalonia.Data.Converters;
using PixelArtEditor.Controls.Settings;
using System;
using System.Globalization;

namespace PixelArtEditor.Converters;

public class TabToViewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => new GeneralView(),
                1 => new AppearanceView(),
                2 => new CanvasView(),
                _ => null,
            };
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
