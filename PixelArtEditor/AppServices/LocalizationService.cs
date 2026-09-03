using Avalonia;
using ExCSS;
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
            ["CrWinBackgroundColor"] = "Background color",
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
            ["Language"] = "English",
            ["TextBlockLayers"] = "Layers",
            ["TextBlockOpacity"] = "Opacity:",
            ["TextBlock"] = "%",
            ["ui"] = "InstantToggleButtonHand: Hand",
            ["TextBlockCheckerboard"] = "Checkerboard",
            ["TextBlockScaleCheckerboardWithCanvas"] = "Scale checkerboard with canvas",
            ["TextBlockCheckerboardScale"] = "Checkerboard Scale",
            ["PanelHorizontal"] = "Horizontal",
            ["GridVertical"] = "Vertical",
            ["controls"] = "ToolbarVertical: Vertical",
            ["PanelVertical"] = "Vertical",
            ["TextBlockPixellerTheBest"] = "Pixeller — the best graphics editor",
            ["RadioButtonRgb"] = "RGB",
            ["RadioButtonHsv"] = "HSV",
            ["TextBlock2"] = "#",
            ["TextBlockR"] = "R",
            ["TextBlockH"] = "H",
            ["TextBlockG"] = "G",
            ["TextBlockS"] = "S",
            ["TextBlockB"] = "B",
            ["TextBlockV"] = "V",
            ["TextBlockA"] = "A",
            ["RadioButtonRgb2"] = "RGB",
            ["RadioButtonHsv2"] = "HSV",
            ["TextBlock3"] = "#",
            ["TextBlockR2"] = "R",
            ["TextBlockH2"] = "H",
            ["TextBlockG2"] = "G",
            ["TextBlockS2"] = "S",
            ["TextBlockB2"] = "B",
            ["TextBlockV2"] = "V",
            ["ListBoxItemCanvas"] = "Canvas",
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
            ["Export"] = "Export",
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
            ["TextBlockLayers2"] = "Layers:",
            ["TextBlockOpacity2"] = "Opacity:",
            ["TextBlock4"] = "%",
            ["TextBlockCheckerboard2"] = "Checkerboard",
            ["TextBlockScaleCheckerboardWithCanvas2"] = "Scale checkerboard with canvas",
            ["TextBlockCheckerboardScale2"] = "Checkerboard Scale",
            ["TextBlockPixellerTheBest2"] = "Pixeller - the best graphics editor",
            ["RadioButtonRgb3"] = "RGB",
            ["RadioButtonHsv3"] = "HSV",
            ["TextBlock5"] = "#",
            ["TextBlockR3"] = "R",
            ["TextBlockH3"] = "H",
            ["TextBlockG3"] = "G",
            ["TextBlockS3"] = "S",
            ["TextBlockB3"] = "B",
            ["TextBlockV3"] = "V",
            ["TextBlockA2"] = "A",
            ["RadioButtonRgb4"] = "RGB",
            ["RadioButtonHsv4"] = "HSV",
            ["TextBlock6"] = "#",
            ["TextBlockR4"] = "R",
            ["TextBlockH4"] = "H",
            ["TextBlockG4"] = "G",
            ["TextBlockS4"] = "S",
            ["TextBlockB4"] = "B",
            ["TextBlockV4"] = "V",
            ["ListBoxItemCanvas2"] = "Canvas",
            ["ToolBarPen"] = "Pen",
            ["ToolBarColorPicker"] = "Color picker",
            ["ToolBarFill"] = "Fill",
            ["ToolBarEraser"] = "Eraser",
            ["ToolBarHand"] = "Hand",
            ["LayerAdd"] = "Add",
            ["LayerDelete"] = "Delete",
            ["LayerDuplicate"] = "Duplicate",
            ["LayerGroup"] = "Group",
            ["LayerTheTop"] = "Move to the top",
            ["LayerTheBottom"] = "Move to the bottom",
            ["HeaderFile"] = "File",
            ["HeaderEdit"] = "Edit",
            ["HeaderView"] = "View",
            ["HeaderHelp"] = "Help",
            ["MenuWindowStateF"] = "Fullscreen",
            ["MenuWindowStateW"] = "Windowed",
            ["Hide"] = "Hide",
            ["Show"] = "Show",
            ["Lock"] = "Lock",
            ["Unlock"] = "Unlock",
            ["TextBlockImage"] = "Image",
            ["TextBlockInterpolationMode"] = "Interpolation mode",
            ["InterpolationNone"] = "None",
            ["InterpolationLow"] = "Low quality",
            ["InterpolationMedium"] = "Medium quality",
            ["InterpolationHigh"] = "High quality",
            ["InterpolateOnlyWhen"] = "Interpolate only when scaling down",
            ["PreviewSquareError"] = "Preview control must be a square."
        };

        foreach (var kvp in dict)
            Application.Current.Resources[kvp.Key] = kvp.Value;

        Directory.CreateDirectory(LocalizationDirectory);
        YamlService.Save(dict, Path.Combine(LocalizationDirectory, "en.yaml"));
    }
}
