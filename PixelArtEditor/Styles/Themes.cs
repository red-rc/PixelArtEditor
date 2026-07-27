using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using System;
using System.IO;
using PixelArtEditor.Helpers;

namespace PixelArtEditor.Styles;

public abstract class BaseTheme
{
    public abstract string Name { get; }
    public abstract ThemeVariant Variant { get; }
    public ResourceDictionary Resources { get; } = [];
    public abstract string StylePath { get; }

    public void Apply()
    {
        if (Application.Current == null)
            throw new InvalidOperationException("Application.Current is null. Make sure Avalonia is initialized.");

        foreach (var key in Resources.Keys)
        {
            Application.Current.Resources[key] = Resources[key];
        }

        EnsureStyleExists();
        var styleInclude = new StyleInclude(new Uri("avares://PixelArtEditor/"))
        {
            Source = new Uri(StylePath)
        };
        Application.Current.Styles.Add(styleInclude);

        Application.Current.RequestedThemeVariant = Variant;
    }

    private static string LoadDefaultStyle()
    {
        using var stream = typeof(BaseTheme).Assembly.GetManifestResourceStream("PixelArtEditor.PixelArtEditor.Styles.SimpleStyle.axaml");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private void EnsureStyleExists()
    {
        string absolutePath = Path.Combine(AppContext.BaseDirectory, StylePath.Replace("avares://PixelArtEditor/", ""));
        string directory = Path.GetDirectoryName(absolutePath)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        if (!File.Exists(absolutePath))
            File.WriteAllText(absolutePath, LoadDefaultStyle());
    }

    public void ChangeAccentColor(Color newColor)
    {
        Resources["PrimaryColor"] = newColor;
        Resources["PrimaryHoverColor"] = ColorHelper.AdjustBrightness(newColor, 0.2);
        Resources["PrimaryPressedColor"] = ColorHelper.AdjustBrightness(newColor, -0.2);

        if (Application.Current != null)
        {
            Application.Current.Resources["PrimaryColor"] = Resources["PrimaryColor"];
            Application.Current.Resources["PrimaryHoverColor"] = Resources["PrimaryHoverColor"];
            Application.Current.Resources["PrimaryPressedColor"] = Resources["PrimaryPressedColor"];
        }
    }

    protected static Bitmap LoadBitmapFromAssets(string avaresRelativePath)
    {
        var uri = new Uri(avaresRelativePath, UriKind.Absolute);
        Stream? s = AssetLoader.Open(uri);
        return new Bitmap(s);
    }

    public void SetDefaults()
    {
        Resources["PrimaryColor"] = Color.Parse("DodgerBlue");
        Resources["PrimaryHoverColor"] = Color.Parse("#4ba6ff");
        Resources["PrimaryPressedColor"] = Color.Parse("#1873cc");

        Resources["MenuFlyoutItemBackground"] = new SolidColorBrush(Colors.Transparent);

        Resources["AppFont"] = new FontFamily("avares://PixelArtEditor/Assets/Fonts/OpenSans-Regular.ttf");
        Resources["HeadingFont"] = new FontFamily("avares://PixelArtEditor/Assets/Fonts/Poppins-SemiBold.ttf");
    }

    public void SetDarkIcons()
    {
        Resources["MinimizeIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/WindowButtons/minimize-icon.png");
        Resources["MaximizeIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/WindowButtons/maximize-icon.png");
        Resources["CloseIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/WindowButtons/close-icon.png");
        Resources["CloseIconHover"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/WindowButtons/close-icon.png");

        Resources["PenIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/EditButtons/pen.png");
        Resources["ColorPickerIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/EditButtons/colorpicker.png");
        Resources["FillIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/EditButtons/fill.png");
        Resources["EraserIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/EditButtons/eraser.png");
        Resources["HandIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/EditButtons/hand.png");

        Resources["AddIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/add.png");
        Resources["RemoveIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/remove.png");
        Resources["DuplicateIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/duplicate.png");
        Resources["GroupIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/group.png");

        Resources["ShowIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/show.png");
        Resources["HideIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/hide.png");
        Resources["LockIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/lock.png");
        Resources["UnlockIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/unlock.png");
        Resources["UpIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/up.png");
        Resources["DownIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/LayerButtons/down.png");

        Resources["ChainIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/UIElements/chain.png");
    }
}

public class DarkTheme : BaseTheme
{
    public override string Name => "Dark";
    public override ThemeVariant Variant => ThemeVariant.Dark;
    public override string StylePath => "avares://PixelArtEditor/Styles/SimpleStyle.axaml";
    public DarkTheme()
    {
        SetDefaults();

        Resources["ForegroundColor"] = Color.Parse("#efefef");
        Resources["DisabledForegroundColor"] = Color.Parse("#8f8f8f");
        Resources["BorderColor"] = Color.Parse("#bfbfbf");

        Resources["BackgroundColor"] = Color.Parse("#1b1b1b");
        Resources["SecondaryBackgroundColor"] = Color.Parse("#222222");
        Resources["TertiaryBackgroundColor"] = Color.Parse("#1e1e1e");
        Resources["BackgroundHoverColor"] = Color.Parse("#383838");
        Resources["BackgroundPressedColor"] = Color.Parse("#141414");

        Resources["GrayColor"] = Colors.DimGray;
        Resources["GrayHoverColor"] = Color.Parse("Gray");
        Resources["GrayPressedColor"] = Color.Parse("#4a4a4a");

        Resources["UiColor"] = Color.Parse("#2e2e2e");
        Resources["UiHoverColor"] = Color.Parse("#444444");
        Resources["UiPressedColor"] = Color.Parse("#181818");

        Resources["MenuFlyoutItemBackgroundPointerOver"] = Resources["UiColor"];
        Resources["MenuFlyoutItemBackgroundPressed"] = Resources["UiColor"];
        Resources["MenuFlyoutPresenterBackground"] = Resources["TertiaryBackgroundColor"];

        Resources["ScrollBackgroundColor"] = Color.Parse("#282828");

        SetDarkIcons();
    }
}

public class LightTheme : BaseTheme
{
    public override string Name => "Light";
    public override ThemeVariant Variant => ThemeVariant.Light;
    public override string StylePath => "avares://PixelArtEditor/Styles/SimpleStyle.axaml";
    public LightTheme()
    {
        SetDefaults();

        Resources["ForegroundColor"] = Color.Parse("#1b1b1b");
        Resources["DisabledForegroundColor"] = Color.Parse("#7a7a7a");
        Resources["BorderColor"] = Color.Parse("#b0b0b0");

        Resources["BackgroundColor"] = Color.Parse("#f0f0f0");
        Resources["SecondaryBackgroundColor"] = Color.Parse("#f7f7f7");
        Resources["TertiaryBackgroundColor"] = Color.Parse("#f5f5f5");
        Resources["BackgroundHoverColor"] = Color.Parse("#e3e3e3");
        Resources["BackgroundPressedColor"] = Color.Parse("#dadada");

        Resources["GrayColor"] = Color.Parse("#a0a0a0");
        Resources["GrayHoverColor"] = Color.Parse("#888888");
        Resources["GrayPressedColor"] = Color.Parse("#666666");

        Resources["UiColor"] = Color.Parse("#e5e5e5");
        Resources["UiHoverColor"] = Color.Parse("#d9d9d9");
        Resources["UiPressedColor"] = Color.Parse("#cecece");

        Resources["MenuFlyoutItemBackgroundPointerOver"] = Resources["UiColor"];
        Resources["MenuFlyoutItemBackgroundPressed"] = Resources["UiColor"];
        Resources["MenuFlyoutPresenterBackground"] = Resources["TertiaryBackgroundColor"];

        Resources["ScrollBackgroundColor"] = Color.Parse("#e0e0e0");

        Resources["MinimizeIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/WindowButtons/minimize-icon.png");
        Resources["MaximizeIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/WindowButtons/maximize-icon.png");
        Resources["CloseIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/WindowButtons/close-icon.png");
        Resources["CloseIconHover"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Dark/WindowButtons/close-icon.png");

        Resources["PenIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/EditButtons/pen.png");
        Resources["ColorPickerIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/EditButtons/colorpicker.png");
        Resources["FillIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/EditButtons/fill.png");
        Resources["EraserIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/EditButtons/eraser.png");
        Resources["HandIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/EditButtons/hand.png");

        Resources["AddIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/add.png");
        Resources["RemoveIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/remove.png");
        Resources["DuplicateIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/duplicate.png");
        Resources["GroupIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/group.png");

        Resources["ShowIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/show.png");
        Resources["HideIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/hide.png");
        Resources["LockIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/lock.png");
        Resources["UnlockIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/unlock.png");
        Resources["UpIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/up.png");
        Resources["DownIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/LayerButtons/down.png");

        Resources["ChainIcon"] = LoadBitmapFromAssets("avares://PixelArtEditor/Assets/Light/UIElements/chain.png");
    }
}

public class GrayTheme : BaseTheme
{
    public override string Name => "Gray";
    public override ThemeVariant Variant => ThemeVariant.Dark;
    public override string StylePath => "avares://PixelArtEditor/Styles/SimpleStyle.axaml";

    public GrayTheme()
    {
        SetDefaults();

        Resources["ForegroundColor"] = Color.Parse("#efefef");
        Resources["DisabledForegroundColor"] = Color.Parse("#8f8f8f");
        Resources["BorderColor"] = Color.Parse("#bfbfbf");

        Resources["BackgroundColor"] = Color.Parse("#2a2a2a");
        Resources["SecondaryBackgroundColor"] = Color.Parse("#323232");
        Resources["TertiaryBackgroundColor"] = Color.Parse("#2e2e2e");
        Resources["BackgroundHoverColor"] = Color.Parse("#484848");
        Resources["BackgroundPressedColor"] = Color.Parse("#1e1e1e");

        Resources["GrayColor"] = Color.Parse("#707070");
        Resources["GrayHoverColor"] = Color.Parse("#888888");
        Resources["GrayPressedColor"] = Color.Parse("#505050");

        Resources["UiColor"] = Color.Parse("#404040");
        Resources["UiHoverColor"] = Color.Parse("#545454");
        Resources["UiPressedColor"] = Color.Parse("#2e2e2e");

        Resources["ScrollBackgroundColor"] = Color.Parse("#383838");

        Resources["MenuFlyoutItemBackgroundPointerOver"] = Resources["UiColor"];
        Resources["MenuFlyoutItemBackgroundPressed"] = Resources["UiColor"];
        Resources["MenuFlyoutPresenterBackground"] = Resources["TertiaryBackgroundColor"];

        SetDarkIcons();
    }
}

public class SystemTheme : BaseTheme
{
    private readonly BaseTheme? _currentTheme;
    public override string Name => "System";
    public override ThemeVariant Variant => _currentTheme?.Variant ?? ThemeVariant.Default;
    public override string StylePath => "avares://PixelArtEditor/Styles/SimpleStyle.axaml";

    public SystemTheme()
    {
        if (Application.Current is null || Application.Current.PlatformSettings is null)
        {
            _currentTheme = null;
            return;
        }

        bool isDark = Application.Current.PlatformSettings.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;
        _currentTheme = isDark ? new DarkTheme() : new LightTheme();

        foreach (var key in _currentTheme.Resources.Keys)
        {
            Resources[key] = _currentTheme.Resources[key];
        }
    }
}