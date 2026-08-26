using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PixelArtEditor.Converters;

public class AddConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && double.TryParse(parameter?.ToString(), out var add))
            return d + add;
        return AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
