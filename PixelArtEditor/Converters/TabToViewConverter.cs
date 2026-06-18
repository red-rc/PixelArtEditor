using Avalonia.Data.Converters;
using PixelArtEditor.Views.SettingsViews;
using System;
using System.Globalization;

namespace PixelArtEditor.Other;

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
                _ => null,
            };
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
