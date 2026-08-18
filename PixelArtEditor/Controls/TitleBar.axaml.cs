using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices;

namespace PixelArtEditor.Controls;

public partial class TitleBar : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
    AvaloniaProperty.Register<TitleBar, string>(nameof(Title), "Window");

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<bool> TitleVisibleProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(TitleVisible), true);

    public bool TitleVisible
    {
        get => GetValue(TitleVisibleProperty);
        set => SetValue(TitleVisibleProperty, value);
    }

    public static readonly StyledProperty<IImage?> IconSourceProperty =
        AvaloniaProperty.Register<TitleBar, IImage?>(nameof(IconSource));

    public IImage? IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public static readonly StyledProperty<bool> IconVisibleProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(IconVisible), true);

    public bool IconVisible
    {
        get => GetValue(IconVisibleProperty);
        set => SetValue(IconVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> CanMinimizeProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(CanMinimize), true);

    public bool CanMinimize
    {
        get => GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    public static readonly StyledProperty<bool> CanMaximizeProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(CanMaximize), true);

    public bool CanMaximize
    {
        get => GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }

    private Window? _dialog;
    private PointerPressedEventArgs? _pressedArgs;

    public static readonly StyledProperty<object?> ExtraContentProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(ExtraContent));

    public object? ExtraContent
    {
        get => GetValue(ExtraContentProperty);
        set => SetValue(ExtraContentProperty, value);
    }

    public TitleBar()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _dialog = this.FindAncestorOfType<Window>();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pressedArgs = e;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedArgs is null || _dialog is null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _dialog.BeginMoveDrag(_pressedArgs);
        _pressedArgs = null;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pressedArgs = null;
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if (_dialog is null) return;

        var isMain = _dialog == (Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (isMain)
            Services.WindowState.Current = WindowState.Minimized;
        else
            _dialog.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        if (_dialog is null) return;

        _dialog.WindowState = _dialog.WindowState switch
        {
            WindowState.Maximized => WindowState.Normal,
            WindowState.Normal => WindowState.Maximized,
            _ => _dialog.WindowState
        };
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => _dialog?.Close();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F11) return;

        e.Handled = true;
        if (Services.WindowState.Current == WindowState.FullScreen)
            Services.WindowState.Current = WindowState.Normal;
        else
            Services.WindowState.Current = WindowState.FullScreen;
    }
}