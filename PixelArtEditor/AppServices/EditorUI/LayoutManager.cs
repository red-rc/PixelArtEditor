using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using PixelArtEditor.Models.Dock;
using PixelArtEditor.Views;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace PixelArtEditor.AppServices.EditorUI;

public class LayoutManager(Grid mainLayout, Panel rectHost, Control canvasPanel)
{
    public static readonly AttachedProperty<DockInfo> DockInfoProperty =
    AvaloniaProperty.RegisterAttached<EditorView, DockInfo>("DockInfo", typeof(EditorView));

    private readonly Grid _mainLayout = mainLayout;
    private readonly Panel _rectHost = rectHost;
    private readonly Control _canvasPanel = canvasPanel;

    public List<Rectangle> VerticalRects { get; private set; } = [];
    public List<Rectangle> HorizontalRects { get; private set; } = [];

    public void LoadLayout()
    {
        foreach (var item in Services.Settings.Layout)
        {
            var control = _mainLayout.Children.OfType<Control>().FirstOrDefault(c => c.Name == item.Name);
            if (control == null) continue;
            Grid.SetRow(control, item.Row);
            Grid.SetColumn(control, item.Col);
        }

        ApplyGridDefinitions();
        UpdateRectPositions();
    }

    public void ApplyGridDefinitions()
    {
        var verticalItems = _mainLayout.Children.OfType<Control>()
            .Where(c => (string?)c.Tag == "Vertical" || c.Name == "CanvasPanel")
            .OrderBy(Grid.GetColumn)
            .ToList();

        for (var i = 0; i < verticalItems.Count && i < _mainLayout.ColumnDefinitions.Count; i++)
        {
            _mainLayout.ColumnDefinitions[i].Width = verticalItems[i].Name == "CanvasPanel"
                ? new GridLength(1, GridUnitType.Star)
                : GridLength.Auto;
        }

        var horizontalItems = _mainLayout.Children.OfType<Control>()
            .Where(c => (string?)c.Tag == "Horizontal" || c.Name == "CanvasPanel")
            .OrderBy(Grid.GetRow)
            .ToList();

        var canvasRow = -1;
        for (var i = 0; i < horizontalItems.Count && i < _mainLayout.RowDefinitions.Count; i++)
        {
            if (horizontalItems[i].Name == "CanvasPanel")
            {
                _mainLayout.RowDefinitions[i].Height = new GridLength(1, GridUnitType.Star);
                canvasRow = i;
            }
            else
                _mainLayout.RowDefinitions[i].Height = GridLength.Auto;
        }

        if (canvasRow >= 0)
        {
            foreach (var child in _mainLayout.Children.OfType<Control>().Where(c => (string?)c.Tag == "Vertical"))
            {
                Grid.SetRow(child, canvasRow);
            }
        }

        _mainLayout.InvalidateMeasure();
    }

    public void InitializeRects()
    {
        _rectHost.Children.Clear();
        VerticalRects = [];
        HorizontalRects = [];

        for (byte i = 0; i <= _mainLayout.ColumnDefinitions.Count; i++)
        {
            var rect = new Rectangle { Width = 3 };

            if (i == 0 || i == _mainLayout.ColumnDefinitions.Count)
                rect.Fill = Brushes.Transparent;
            else
                rect.Bind(
                    Shape.FillProperty,
                    Observable.Select(
                        rect.GetResourceObservable("BackgroundPressedColor"),
                        c => c is Color color ? (IBrush)new SolidColorBrush(color) : Brushes.DarkGray
                    )
                );

            rect.SetValue(DockInfoProperty, new DockInfo
            {
                Index = i,
                Orientation = DockOrientation.Vertical
            });

            _rectHost.Children.Add(rect);
            VerticalRects.Add(rect);
        }

        for (byte i = 0; i <= _mainLayout.RowDefinitions.Count; i++)
        {
            var rect = new Rectangle { Height = 3 };

            if (i == 0 || i == _mainLayout.RowDefinitions.Count)
                rect.Fill = Brushes.Transparent;
            else
                rect.Bind(
                    Shape.FillProperty,
                    Observable.Select(
                        rect.GetResourceObservable("BackgroundPressedColor"),
                        c => c is Color color ? (IBrush)new SolidColorBrush(color) : Brushes.DarkGray
                    )
                );

            rect.SetValue(DockInfoProperty, new DockInfo
            {
                Index = i,
                Orientation = DockOrientation.Horizontal
            });

            _rectHost.Children.Add(rect);
            HorizontalRects.Add(rect);
        }

        UpdateRectPositions();
    }

    public void UpdateRectPositions()
    {
        double x = 0;
        double y = 0;
        double canvasRowTop = 0;

        var canvasRow = Grid.GetRow(_canvasPanel);

        for (var r = 0; r < canvasRow; r++)
            canvasRowTop += _mainLayout.RowDefinitions[r].ActualHeight;

        for (byte i = 0; i <= _mainLayout.ColumnDefinitions.Count; i++)
        {
            if (i > 0)
                x += _mainLayout.ColumnDefinitions[i - 1].ActualWidth;

            var rect = VerticalRects[i];

            if (i == 0)
                Avalonia.Controls.Canvas.SetLeft(rect, x);
            else if (i == _mainLayout.ColumnDefinitions.Count)
                Avalonia.Controls.Canvas.SetLeft(rect, x - rect.Width);
            else
                Avalonia.Controls.Canvas.SetLeft(rect, x - rect.Width / 2);

            Avalonia.Controls.Canvas.SetTop(rect, canvasRowTop);
            rect.Height = _mainLayout.RowDefinitions[canvasRow].ActualHeight;
        }

        for (byte i = 0; i <= _mainLayout.RowDefinitions.Count; i++)
        {
            if (i > 0)
                y += _mainLayout.RowDefinitions[i - 1].ActualHeight;

            var rect = HorizontalRects[i];

            if (i == 0)
                Avalonia.Controls.Canvas.SetTop(rect, y);
            else if (i == _mainLayout.RowDefinitions.Count)
                Avalonia.Controls.Canvas.SetTop(rect, y - rect.Height);
            else
                Avalonia.Controls.Canvas.SetTop(rect, y - rect.Height / 2);

            Avalonia.Controls.Canvas.SetLeft(rect, 0);
            rect.Width = _mainLayout.Bounds.Width;
        }
    }
}
