using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Pixeller.Models.Canvas;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Pixeller.Controls.Editor;

public partial class SwatchItem(int index) : ReactiveObject
{
    public int Index { get; } = index;
    [Reactive] public partial Color Color { get; set; }
    [Reactive] public partial bool IsUsed { get; set; }
}

public partial class PalettePanel : UserControl
{
    public const int SwatchCount = 256;

    public IReadOnlyList<SwatchItem> Swatches { get; } =
        [.. Enumerable.Range(0, SwatchCount).Select(i => new SwatchItem(i))];

    public static readonly StyledProperty<Palette?> ImagePaletteProperty =
        AvaloniaProperty.Register<PalettePanel, Palette?>(nameof(ImagePalette));

    public Palette? ImagePalette
    {
        get => GetValue(ImagePaletteProperty);
        set => SetValue(ImagePaletteProperty, value);
    }

    private readonly ColorView _picker = new()
    {
        IsColorPaletteVisible = false,
        IsAlphaEnabled = false,
        IsAlphaVisible = false
    };
    private readonly Flyout _flyout;
    private SwatchItem? _editing;
    private bool _loading;

    public PalettePanel()
    {
        InitializeComponent();

        _flyout = new Flyout
        {
            Placement = PlacementMode.Top,
            Content = new Border 
            { 
                Padding = new Thickness(6), 
                Child = new Border
                { 
                    Classes = { "ShadowBorder" }, 
                    CornerRadius = new CornerRadius(5), 
                    Child = _picker 
                } 
            }
        };

        ImagePaletteProperty.Changed.AddClassHandler<PalettePanel>((p, _) => p.Load());

        _picker.GetObservable(ColorView.ColorProperty).Subscribe(c =>
        {
            if (!_loading && _editing is not null) _editing.Color = c;
        });

        foreach (var swatch in Swatches)
        {
            swatch.WhenAnyValue(x => x.Color).Skip(1).Subscribe(c =>
            {
                if (_loading || !swatch.IsUsed || ImagePalette is not { } palette) return;
                palette.Colors[swatch.Index] = c;
                palette.Cube = null;
            });
        }
    }

    private void Load()
    {
        _loading = true;

        var palette = ImagePalette;
        for (var i = 0; i < SwatchCount; i++)
        {
            var used = palette is not null && i < palette.Colors.Count;
            Swatches[i].IsUsed = used;
            Swatches[i].Color = used ? palette!.Colors[i] : Colors.Transparent;
        }

        _loading = false;
    }

    private void OnSwatchPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border { DataContext: SwatchItem swatch } border || ImagePalette is not { } palette) return;

        if (!swatch.IsUsed && palette.Colors.Count < SwatchCount)
        {
            swatch = Swatches[palette.Colors.Count];
            palette.Colors.Add(Colors.Red);
            palette.Cube = null;
            _loading = true;
            swatch.IsUsed = true;
            swatch.Color = Colors.Red;
            _loading = false;
        }

        _loading = true;
        _editing = swatch;
        _picker.Color = swatch.Color;
        _loading = false;

        _flyout.ShowAt(border);
    }
}
