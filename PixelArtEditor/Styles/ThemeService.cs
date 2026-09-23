using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using PixelArtEditor.AppServices;
using PixelArtEditor.Helpers;
using System;
using System.Collections.Generic;
using ColorHelper = PixelArtEditor.Helpers.ColorHelper;

namespace PixelArtEditor.Styles;

public class ThemeData
{
    public string Name { get; set; } = "";
    public string Variant { get; set; } = "";
    public Dictionary<string, string> Colors { get; set; } = [];
    public string AppFontSource { get; set; } = ""; 
    public string AppFontFamilyName { get; set; } = "";
    public string HeadingFontSource { get; set; } = "";
    public string HeadingFontFamilyName { get; set; } = "";
}

public class BaseTheme
{
    public string Name { get; set; } = "";
    public ThemeVariant Variant { get; set; } = ThemeVariant.Dark;
    public ThemeData Resources { get; } = new();

    public void Apply()
    {
        if (Application.Current is null)
            throw new InvalidOperationException(LocalizationService.Get("ApplicationCurrentNull"));

        ThemeHelper.ApplyFromHexDict(Application.Current.Resources, Resources.Colors);
        ThemeHelper.ApplyFonts(Application.Current.Resources, Resources.AppFontSource, 
            Resources.AppFontFamilyName, Resources.HeadingFontSource, Resources.HeadingFontFamilyName);

        ApplyDerived();

        if (Variant == ThemeVariant.Dark) SetDarkIcons();
        else SetLightIcons();

        Application.Current.RequestedThemeVariant = Variant;
    }

    public void ApplyDerived()
    {
        var resources = Application.Current!.Resources;

        resources["MenuFlyoutItemBackgroundPointerOver"] = new SolidColorBrush(ColorHelper.HexToColor(Resources.Colors["UiColor"]));
        resources["MenuFlyoutItemBackgroundPressed"] = new SolidColorBrush(ColorHelper.HexToColor(Resources.Colors["UiColor"]));
        resources["MenuFlyoutPresenterBackground"] = new SolidColorBrush(ColorHelper.HexToColor(Resources.Colors["TertiaryBackgroundColor"]));

        resources["ThemeBorderHighBrush"] = new SolidColorBrush(ColorHelper.HexToColor(Resources.Colors["BorderColor"]));
        resources["ThemeBackgroundBrush"] = new SolidColorBrush(ColorHelper.HexToColor(Resources.Colors["UiColor"]));
        resources["ThemeBorderMidBrush"] = new SolidColorBrush(ColorHelper.HexToColor(Resources.Colors["ShadowColor"]));
        resources["ThemeBorderLowBrush"] = new SolidColorBrush(ColorHelper.HexToColor(Resources.Colors["GrayColor"]));

        resources["CardShadow"] = new BoxShadows(new BoxShadow 
        { 
            OffsetX = 0, 
            OffsetY = 0, 
            Blur = 6, 
            Spread = 2, 
            Color = ColorHelper.HexToColor(Resources.Colors["ShadowColor"])
        });
    }

    public void ChangeAccentColor(string newColor)
    {
        Resources.Colors["PrimaryColor"] = newColor;
        Resources.Colors["PrimaryHoverColor"] = ColorHelper.AdjustBrightness(newColor, 0.2);
        Resources.Colors["PrimaryPressColor"] = ColorHelper.AdjustBrightness(newColor, -0.2);

        if (Application.Current is not null)
        {
            Application.Current.Resources["PrimaryColor"] = Color.Parse(Resources.Colors["PrimaryColor"]);
            Application.Current.Resources["PrimaryHoverColor"] = Color.Parse(Resources.Colors["PrimaryHoverColor"]);
            Application.Current.Resources["PrimaryPressColor"] = Color.Parse(Resources.Colors["PrimaryPressColor"]);
        }
    }

    public void SetDefaults()
    {
        Resources.Colors["PrimaryColor"] = "#1e90ff";
        Resources.Colors["PrimaryHoverColor"] = "#4ba6ff";
        Resources.Colors["PrimaryPressColor"] = "#1873cc";

        Resources.Colors["MenuFlyoutItemBackground"] = "#00ffffff";

        Resources.AppFontSource = "Styles/Fonts/Manrope-Medium.ttf";
        Resources.AppFontFamilyName = "Manrope Medium";
        Resources.HeadingFontSource = "Styles/Fonts/Montserrat-SemiBold.ttf";
        Resources.HeadingFontFamilyName = "Montserrat SemiBold";
    }

    public static void SetDarkIcons()
    {
        var resources = (Application.Current?.Resources)
            ?? throw new InvalidOperationException(LocalizationService.Get("ApplicationCurrentNull"));

        resources["MinimizeIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/WindowButtons/minimize-icon.png");
        resources["MaximizeIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/WindowButtons/maximize-icon.png");
        resources["CloseIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/WindowButtons/close-icon.png");
        resources["CloseIconHover"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/WindowButtons/close-icon.png");

        resources["PenIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/EditButtons/pen.png");
        resources["ColorPickerIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/EditButtons/colorpicker.png");
        resources["FillIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/EditButtons/fill.png");
        resources["EraserIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/EditButtons/eraser.png");
        resources["HandIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/EditButtons/hand.png");

        resources["AddIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/add.png");
        resources["DeleteIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/delete.png");
        resources["DuplicateIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/duplicate.png");
        resources["GroupIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/group.png");

        resources["ShowIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/show.png");
        resources["HideIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/hide.png");
        resources["LockIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/lock.png");
        resources["UnlockIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/unlock.png");
        resources["UpIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/up.png");
        resources["DownIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/LayerButtons/down.png");

        resources["ChainIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/UIElements/chain.png");
        resources["CancelIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/UIElements/cancel.png");
        resources["RedoIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/UIElements/redo.png");
        resources["UndoIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/UIElements/undo.png");
        resources["UploadIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/UIElements/upload.png");
        resources["CheckMarkIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/UIElements/checkMark.png");
        resources["GearIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/UIElements/gear.png");
    }

    public static void SetLightIcons()
    {
        var resources = (Application.Current?.Resources)
            ?? throw new InvalidOperationException(LocalizationService.Get("ApplicationCurrentNull"));

        resources["MinimizeIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/WindowButtons/minimize-icon.png");
        resources["MaximizeIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/WindowButtons/maximize-icon.png");
        resources["CloseIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/WindowButtons/close-icon.png");
        resources["CloseIconHover"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Dark/WindowButtons/close-icon.png");

        resources["PenIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/EditButtons/pen.png");
        resources["ColorPickerIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/EditButtons/colorpicker.png");
        resources["FillIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/EditButtons/fill.png");
        resources["EraserIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/EditButtons/eraser.png");
        resources["HandIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/EditButtons/hand.png");

        resources["AddIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/add.png");
        resources["DeleteIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/delete.png");
        resources["DuplicateIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/duplicate.png");
        resources["GroupIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/group.png");

        resources["ShowIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/show.png");
        resources["HideIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/hide.png");
        resources["LockIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/lock.png");
        resources["UnlockIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/unlock.png");
        resources["UpIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/up.png");
        resources["DownIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/LayerButtons/down.png");

        resources["ChainIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/UIElements/chain.png");
        resources["CancelIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/UIElements/cancel.png");
        resources["RedoIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/UIElements/redo.png");
        resources["UndoIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/UIElements/undo.png");
        resources["UploadIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/UIElements/upload.png");
        resources["CheckMarkIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/UIElements/checkMark.png");
        resources["GearIcon"] = ThemeHelper.LoadBitmap("avares://PixelArtEditor/Assets/Light/UIElements/gear.png");
    }
}

public static class DefaultThemes
{
    public static BaseTheme CreateDark()
    {
        var theme = new BaseTheme { Name = "Dark", Variant = ThemeVariant.Dark };
        theme.SetDefaults();

        theme.Resources.Colors["ForegroundColor"] = "#efefef";
        theme.Resources.Colors["DisabledForegroundColor"] = "#8f8f8f";
        theme.Resources.Colors["BorderColor"] = "#bfbfbf";

        theme.Resources.Colors["BackgroundColor"] = "#1b1b1b";
        theme.Resources.Colors["SecondaryBackgroundColor"] = "#222222";
        theme.Resources.Colors["TertiaryBackgroundColor"] = "#1e1e1e";
        theme.Resources.Colors["BackgroundHoverColor"] = "#383838";
        theme.Resources.Colors["BackgroundPressColor"] = "#141414";

        theme.Resources.Colors["GrayColor"] = "#696969";
        theme.Resources.Colors["GrayHoverColor"] = "#888888";
        theme.Resources.Colors["GrayPressColor"] = "#4a4a4a";

        theme.Resources.Colors["UiColor"] = "#2e2e2e";
        theme.Resources.Colors["UiHoverColor"] = "#444444";
        theme.Resources.Colors["UiPressColor"] = "#282828";

        theme.Resources.Colors["ScrollBackgroundColor"] = "#282828";

        theme.Resources.Colors["ShadowColor"] = "#2B000000";
        return theme;
    }

    public static BaseTheme CreateLight()
    {
        var theme = new BaseTheme { Name = "Light", Variant = ThemeVariant.Light };
        theme.SetDefaults();

        theme.Resources.Colors["ForegroundColor"] = "#101010";
        theme.Resources.Colors["DisabledForegroundColor"] = "#7a7a7a";
        theme.Resources.Colors["BorderColor"] = "#b0b0b0";

        theme.Resources.Colors["BackgroundColor"] = "#f0f0f0";
        theme.Resources.Colors["SecondaryBackgroundColor"] = "#f7f7f7";
        theme.Resources.Colors["TertiaryBackgroundColor"] = "#f5f5f5";
        theme.Resources.Colors["BackgroundHoverColor"] = "#e3e3e3";
        theme.Resources.Colors["BackgroundPressColor"] = "#dadada";

        theme.Resources.Colors["GrayColor"] = "#a0a0a0";
        theme.Resources.Colors["GrayHoverColor"] = "#888888";
        theme.Resources.Colors["GrayPressColor"] = "#666666";

        theme.Resources.Colors["UiColor"] = "#e5e5e5";
        theme.Resources.Colors["UiHoverColor"] = "#d9d9d9";
        theme.Resources.Colors["UiPressColor"] = "#cecece";

        theme.Resources.Colors["ScrollBackgroundColor"] = "#e0e0e0";

        theme.Resources.Colors["ShadowColor"] = "#2B000000";
        return theme;
    }

    public static BaseTheme CreateGray()
    {
        var theme = new BaseTheme { Name = "Gray", Variant = ThemeVariant.Dark };
        theme.SetDefaults();

        theme.Resources.Colors["ForegroundColor"] = "#efefef";
        theme.Resources.Colors["DisabledForegroundColor"] = "#8f8f8f";
        theme.Resources.Colors["BorderColor"] = "#bfbfbf";

        theme.Resources.Colors["BackgroundColor"] = "#2a2a2a";
        theme.Resources.Colors["SecondaryBackgroundColor"] = "#323232";
        theme.Resources.Colors["TertiaryBackgroundColor"] = "#2e2e2e";
        theme.Resources.Colors["BackgroundHoverColor"] = "#484848";
        theme.Resources.Colors["BackgroundPressColor"] = "#1e1e1e";

        theme.Resources.Colors["GrayColor"] = "#707070";
        theme.Resources.Colors["GrayHoverColor"] = "#888888";
        theme.Resources.Colors["GrayPressColor"] = "#505050";

        theme.Resources.Colors["UiColor"] = "#404040";
        theme.Resources.Colors["UiHoverColor"] = "#545454";
        theme.Resources.Colors["UiPressColor"] = "#2e2e2e";

        theme.Resources.Colors["ScrollBackgroundColor"] = "#383838";

        theme.Resources.Colors["ShadowColor"] = "#2B000000";
        return theme;
    }

    public static BaseTheme CreateSystem()
    {
        bool isDark = Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;
        var baseTheme = isDark ? CreateDark() : CreateLight();
        baseTheme.Name = "System";
        return baseTheme;
    }
}