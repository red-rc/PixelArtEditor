using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace PixelArtEditor.Other;

public class DivideByConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && double.TryParse(parameter?.ToString(), out var divisor))
        {
            return d / divisor;
        }
        return AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}