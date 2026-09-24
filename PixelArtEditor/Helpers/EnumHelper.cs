using PixelArtEditor.AppServices;
using System;

namespace PixelArtEditor.Helpers;

public static class EnumHelper
{
    public static T StringToEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, ignoreCase: false, out var result))
            return result;

        throw new ArgumentException($"{LocalizationService.Get("UnknownValue")} '{value}' " +
            $"{LocalizationService.Get("ForEnum")} {typeof(T).Name}");
    }
}