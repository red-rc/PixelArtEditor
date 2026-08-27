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
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || Services.WindowState.Current == WindowState.FullScreen) return;

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
        Services.WindowState.Current = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        if (_dialog is null) return;

        Services.WindowState.Current = Services.WindowState.Current switch
        {
            WindowState.Maximized => WindowState.Normal,
            WindowState.FullScreen => WindowState.Normal,
            WindowState.Normal => WindowState.Maximized,
            _ => Services.WindowState.Current
        };
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => _dialog?.Close();
}