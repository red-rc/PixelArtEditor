using Avalonia;
using System;
using System.Collections.Generic;

namespace PixelArtEditor.AppServices;

public class LocalizationService : ILocalizationService
{
    public void SetLanguage(string langCode)
    {
        try
        {
            Load(Services.Yaml.Load("Localization/" + langCode + ".yaml"));
        }
        catch (Exception)
        {
            try
            {
                Load(Services.Yaml.Load("Localization/en.yaml"));
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

    public void SetDefaults()
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
            ["MenuLightTheme"] = "Light Theme",
            ["MenuDarkTheme"] = "Dark Theme",

            ["MenuCheckForUpdates"] = "Check For Updates",
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

            ["EditorViewAlignTop"] = "Align to the top",
            ["EditorViewAlignBottom"] = "Align to the bottom",
            ["EditorViewAlignRight"] = "Align to the right",
            ["EditorViewAlignLeft"] = "Align to the left",
            ["EditorViewBringToFront"] = "Bring to front",
            ["EditorViewBringToBack"] = "Bring to back",

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

            ["Language"] = "English"
        };

        foreach (var kvp in dict)
            Application.Current.Resources[kvp.Key] = kvp.Value;

        Services.Yaml.Save(dict, "Localization/en.yaml");
    }
}
