using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using System;
using System.Linq;

namespace PixelArtEditor.Helpers;

public static class LayerNameHelper
{
    private static readonly string Copy = LocalizationService.Get("Copy");
    private static readonly string CopySuffix = $" - {Copy} (";
    public static string GetLayerName(LayerManager layerManager, string baseName)
    {
        string name;

        var trimmedName = baseName.Trim();
        if (!NameExists(layerManager, trimmedName)) return trimmedName;

        if (!trimmedName.Contains(CopySuffix))
        {
            if (trimmedName.EndsWith($" - {Copy}"))
                name = $"{trimmedName} (2)";
            else
                name = $"{trimmedName} - {Copy}";

            var copyCount = 1;
            while (NameExists(layerManager, name))
            {
                if (name.EndsWith($" - {Copy}"))
                {
                    copyCount++;
                    name = $"{name} ({copyCount})";
                }
                else
                    IncreaseIndex(layerManager, ref name);
            }
        }

        else
        {
            name = trimmedName;
            IncreaseIndex(layerManager, ref name);
        }

        return name;
    }

    private static void IncreaseIndex(LayerManager layerManager, ref string name)
    {
        if (name.EndsWith(')') && int.TryParse(name.AsSpan(name.LastIndexOf(CopySuffix) + CopySuffix.Length,
                name.Length - name.LastIndexOf(CopySuffix) - CopySuffix.Length - 1), out int copyNumber))
        {
            while (NameExists(layerManager, name))
            {
                copyNumber++;
                name = $"{name[..(name.LastIndexOf(CopySuffix) + CopySuffix.Length)]}{copyNumber})";
            }
        }
    }

    private static bool NameExists(LayerManager layerManager, string name)
        => layerManager.Layers.Any(l => l.Name == name);
}
