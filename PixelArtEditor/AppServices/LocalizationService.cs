using Avalonia;
using PixelArtEditor.AppServices.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace PixelArtEditor.AppServices;

public static class LocalizationService
{
    private static string LocalizationDirectory => Path.Combine(AppContext.BaseDirectory, "Localization");

    public static void SetLanguage(string langCode)
    {
        try
        {
            Load(YamlService.Load(Path.Combine(LocalizationDirectory, langCode + ".yaml")));
        }
        catch (Exception)
        {
            try
            {
                Load(YamlService.Load(Path.Combine(LocalizationDirectory, "en.yaml")));
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
            throw new InvalidOperationException($"{Get("ApplicationCurrentNull")}");

        foreach (var key in langDict.Keys)
            Application.Current.Resources[key] = langDict[key];
    }

    public static string Get(string key) =>
        Application.Current?.Resources[key] as string ?? key;

    public static void SetDefaults()
    {
        if (Application.Current == null)
            throw new InvalidOperationException($"{Get("ApplicationCurrentNull")}");

        var dict = new Dictionary<string, string>
        {
            ["CreateNew"] = "Create New",
            ["Open"] = "Open",
            ["Import"] = "Import",
            ["Save"] = "Save",
            ["SaveAs"] = "Save As",
            ["Export"] = "Export",
            ["LastAutosave"] = "Last Autosave",
            ["Exit"] = "Exit",
            ["Undo"] = "Undo",
            ["Redo"] = "Redo",
            ["ImageProperties"] = "Image Properties",
            ["Settings"] = "Settings",
            ["ZoomIn"] = "Zoom In",
            ["ZoomOut"] = "Zoom Out",
            ["ResetZoom"] = "Reset Zoom",
            ["ResetLayout"] = "Reset Layout",
            ["LightTheme"] = "Light Theme",
            ["DarkTheme"] = "Dark Theme",
            ["CheckForUpdates"] = "Check For Updates",
            ["Report"] = "Report An Issue",
            ["ContactUs"] = "Contact Us",
            ["About"] = "About",
            ["Title"] = "Create File",
            ["Width"] = "Width",
            ["Height"] = "Height",
            ["Format"] = "Format",
            ["BackgroundColorStr"] = "Background color",
            ["Cancel"] = "Cancel",
            ["Create"] = "Create",
            ["General"] = "General",
            ["Appearance"] = "Appearance",
            ["Reset"] = "Reset",
            ["Theme"] = "Theme",
            ["AccentColor"] = "Accent color",
            ["Language"] = "Language",
            ["PixelGrid"] = "Pixel Grid",
            ["PixelGridMaxSize"] = "Pixel grid max size",
            ["PixelGridColor"] = "Pixel grid color",
            ["EnablePixelGrid"] = "Enable pixel grid",
            ["Autosave"] = "Autosave",
            ["EnableAutosave"] = "Enable autosave",
            ["AutosaveEvery"] = "Autosave every",
            ["Seconds"] = "seconds",
            ["DpiXY"] = "DPI (x, y)",
            ["ColorMode"] = "Color Mode",
            ["BitDepth"] = "Bit Depth",
            ["AlphaFormat"] = "Alpha Format",
            ["ColorSpaces"] = "Color Spaces",
            ["BigEndian"] = "Big Endian",
            ["AppVersion"] = "v1.0.0",
            ["Basic"] = "Basic",
            ["Advanced"] = "Advanced",
            ["Confirm"] = "Confirm",
            ["Size"] = "Size",
            ["Layers"] = "Layers",
            ["Opacity"] = "Opacity:",
            ["ui"] = "InstantToggleButtonHand: Hand",
            ["Checkerboard"] = "Checkerboard",
            ["ScaleCheckerboardWithCanvas"] = "Scale checkerboard with canvas",
            ["CheckerboardScale"] = "Checkerboard scale",
            ["PanelHorizontal"] = "Horizontal",
            ["GridVertical"] = "Vertical",
            ["controls"] = "ToolbarVertical: Vertical",
            ["PixellerTheBest"] = "Pixeller — the best graphics editor",
            ["Rgb"] = "RGB",
            ["Hsv"] = "HSV",
            ["R"] = "R",
            ["H"] = "H",
            ["G"] = "G",
            ["S"] = "S",
            ["B"] = "B",
            ["V"] = "V",
            ["A"] = "A",
            ["Canvas"] = "Canvas",
            ["ViewLocalotNotFound"] = "Not Found",
            ["InvalidBitmap"] = "Invalid bitmap format.",
            ["InvalidPixelData"] = "PixelData size mismatch with bitmap size.",
            ["Layer"] = "Layer",
            ["SupportedFormats"] = "All Supported Images",
            ["Image"] = "Image",
            ["ImportImage"] = "Import image",
            ["InvalidSVG"] = "Invalid or empty SVG file.",
            ["InvalidDDS"] = "Unsupported or invalid DDS format.",
            ["InvalidICO"] = "Invalid .ico file.",
            ["UnsupportedImage"] = "Unsupported image format.",
            ["FileCorrupted"] = "The file is corrupted or contains invalid content.",
            ["CantRead"] = "Could not read image information.",
            ["Untitled"] = "untitled",
            ["FailedExport"] = "Failed to export image.",
            ["NotImplemented"] = "not yet implemented.",
            ["NoPalette"] = "Indexed mode doesn't have palette.",
            ["PfimFailed"] = "Pfim decode failed",
            ["UnhandledPfim"] = "Unhandled Pfim format",
            ["FileNotFound"] = "File not found",
            ["EmptyConfig"] = "Config file is empty.",
            ["Copy"] = "Copy",
            ["UnknownValue"] = "Unknown value",
            ["ForEnum"] = "for enum",
            ["Layers2"] = "Layers:",
            ["Pen"] = "Pen",
            ["ColorPicker"] = "Color picker",
            ["Fill"] = "Fill",
            ["Eraser"] = "Eraser",
            ["Hand"] = "Hand",
            ["Add"] = "Add",
            ["Delete"] = "Delete",
            ["Duplicate"] = "Duplicate",
            ["Group"] = "Group",
            ["ToTheTop"] = "Move to the top",
            ["ToTheBottom"] = "Move to the bottom",
            ["File"] = "File",
            ["Edit"] = "Edit",
            ["View"] = "View",
            ["Help"] = "Help",
            ["FullScreen"] = "Fullscreen",
            ["Windowed"] = "Windowed",
            ["Hide"] = "Hide",
            ["Show"] = "Show",
            ["Lock"] = "Lock",
            ["Unlock"] = "Unlock",
            ["InterpolationMode"] = "Interpolation mode",
            ["InterpolationNone"] = "None",
            ["InterpolationLow"] = "Low quality",
            ["InterpolationMedium"] = "Medium quality",
            ["InterpolationHigh"] = "High quality",
            ["InterpolateOnlyWhen"] = "Interpolate only when scaling down",
            ["PreviewSquareError"] = "Preview control must be square.",
            ["ApplicationCurrentNull"] = "Application.Current is null. Make sure Avalonia is initialized."
        };

        foreach (var key in dict.Keys)
        {
            if (Application.Current.Resources.TryGetValue(key, out var existing) && existing is not string)
                continue;
            Application.Current.Resources[key] = dict[key];
        }

        Directory.CreateDirectory(LocalizationDirectory);
        YamlService.Save(dict, Path.Combine(LocalizationDirectory, "en.yaml"));
    }
}
