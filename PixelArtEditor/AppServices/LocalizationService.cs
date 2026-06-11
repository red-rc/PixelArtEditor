using Avalonia;
using PixelArtEditor.AppServices.Serialization;
using System;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices;

public static class LocalizationService
{
    public static void SetLanguage(string langCode)
    {
        try
        {
            Load(YamlService.Load("Localization/" + langCode + ".yaml"));
        }
        catch (Exception)
        {
            try
            {
                Load(YamlService.Load("Localization/en.yaml"));
            }
            catch (Exception)
            {
                SetDefaults();
            }
        }
    }

    private static void Load(Dictionary<string, string> langDict)
    {
        if (Application.Current == null)
            throw new InvalidOperationException("Application.Current is null. Make sure Avalonia is initialized.");

        foreach (var key in langDict.Keys)
        {
            Application.Current.Resources[key] = langDict[key];
        }
    }

    public static void SetDefaults()
    {
        if (Application.Current == null)
            throw new InvalidOperationException("Application.Current is null. Make sure Avalonia is initialized.");

        var dict = new Dictionary<string, string>
        {
            ["MenuCreateNew"] = "Create New",
            ["MenuOpen"] = "Open",
            ["MenuImport"] = "Import",
            ["MenuSave"] = "Save",
            ["MenuSaveAs"] = "Save As",
            ["MenuExport"] = "Export",
            ["MenuLastAutosave"] = "Last Autosave",
            ["MenuExit"] = "Exit",

            ["MenuUndo"] = "Undo",
            ["MenuRedo"] = "Redo",
            ["MenuImageProperties"] = "Image Properties",
            ["MenuSettings"] = "Settings",

            ["MenuZoomIn"] = "Zoom In",
            ["MenuZoomOut"] = "Zoom Out",
            ["MenuResetZoom"] = "Reset Zoom",
            ["MenuResetLayout"] = "Reset Layout",
            ["MenuLightTheme"] = "Light Theme",
            ["MenuDarkTheme"] = "Dark Theme",

            ["MenuCheckForUpdates"] = "Check For Updates",
            ["Report"] = "Report An Issue",
            ["MenuContactUs"] = "Contact Us",
            ["MenuAbout"] = "About",

            ["CrWinTitle"] = "Create File",
            ["CrWinWidth"] = "Width",
            ["CrWinHeight"] = "Height",
            ["CrWinFormat"] = "Format",
            ["CrWinBackgroundColor"] = "Background\nColor",
            ["CrWinCancel"] = "Cancel",
            ["CrWinCreate"] = "Create",

            ["SetWinTitle"] = "Settings",
            ["SetWinGeneral"] = "General",
            ["SetWinAppearance"] = "Appearance",
            ["SetWinCancel"] = "Cancel",
            ["SetWinReset"] = "Reset",
            ["SetWinSave"] = "Save",

            ["StartViewCreate"] = "Create",
            ["StartViewOpen"] = "Open",
            ["StartViewText"] = "Pixeller - your pixel art editor",

            ["AprViewTheme"] = "Theme",
            ["AprViewAccentColor"] = "Accent Color",

            ["GenViewLanguage"] = "Language",
            ["GenViewGrid"] = "Pixel Grid",
            ["GenViewGridMaxSize"] = "Pixel grid max size",
            ["GenViewGridColor"] = "Pixel grid Color",
            ["GenViewEnableGrid"] = "Enable Grid",
            ["GenViewAutosave"] = "Autosave",
            ["GenViewEnableAutosave"] = "Enable Autosave",
            ["GenViewAutosaveEvery"] = "Autosave every",
            ["GenViewSeconds"] = "seconds",

            ["ImgPropDpiXY"] = "DPI (x, y)",
            ["ImgPropColorMode"] = "Color Mode",
            ["ImgPropBitDepth"] = "Bit Depth",
            ["ImgPropAlphaFormat"] = "Alpha Format",
            ["ImgPropColorSpaces"] = "Color Spaces",
            ["ImgPropBigEndian"] = "Big Endian",
            ["AppVersion"] = "v1.0.0",
            ["CrWinBasic"] = "Basic",
            ["CrWinAdvanced"] = "Advanced",
            ["ExportConfirm"] = "Confirm",
            ["ImgPropSize"] = "Size",

            ["Language"] = "English"
        };

        foreach (var kvp in dict)
            Application.Current.Resources[kvp.Key] = kvp.Value;

        YamlService.Save(dict, "Localization/en.yaml");
    }
}
