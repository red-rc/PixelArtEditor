using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using PixelArtEditor.AppServices;
using PixelArtEditor.Other;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.Views;

public partial class EditorView : UserControl
{
    private bool _dragging;
    private DockState? _dockState;

    private class DockInfo
    {
        public byte Index { get; set; }
        public DockOrientation Orientation { get; set; }
    }

    private static readonly AttachedProperty<DockInfo> DockInfoProperty =
        AvaloniaProperty.RegisterAttached<EditorView, DockInfo>("DockInfo", typeof(EditorView));

    private bool _pointerDown;

    private List<Rectangle> _verticalRects = [];
    private List<Rectangle> _horizontalRects = [];

    private CancellationTokenSource? _tooltipCts;
    private string? _currentTooltipTag;
    private Point _lastPointerPos;

    private EditorVM? ViewModel => DataContext as EditorVM;

    public EditorView()
    {
        InitializeComponent();
        AttachedToVisualTree += (s, e) =>
        {
            if (DataContext is EditorVM vm)
            {
                vm.SetCanvas(CanvasControl);
            }

            Services.ImageData.ModelChanged += _ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ViewModel is null || CanvasPanel.Bounds is not { Width: > 0, Height: > 0 }) return;
                    ViewModel.AdjustCanvas(CanvasPanel.Bounds.Width, CanvasPanel.Bounds.Height);
                });
            };

            Services.Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(SettingsService.Layout)) return;
                LoadLayout();
            };

            InitializeRects();
            LoadLayout();

            MainLayout.LayoutUpdated += OnMainLayoutLayoutUpdated;
        };
    }

    private void CanvasPanel_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (ViewModel == null || e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;
        ViewModel.AdjustCanvas(e.NewSize.Width, e.NewSize.Height);
    }

    private void CanvasPanel_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel == null || e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed) return;

        var rawScale = ViewModel.Scale + e.Delta.Y * ViewModel.Scale / 10.0;

        var newScale = Math.Clamp(rawScale, ViewModel.MinScale, ViewModel.MaxScale);

        if (Math.Abs(newScale - ViewModel.Scale) < 1e-9) return;

        var mousePos = e.GetPosition(CanvasPanel);
        var center = new Point(CanvasPanel.Bounds.Width / 2, CanvasPanel.Bounds.Height / 2);

        var screenVec = new Vector2(
            (float)mousePos.X - (float)center.X - ViewModel.Offset.X,
            (float)mousePos.Y - (float)center.Y - ViewModel.Offset.Y);

        var newScreenVec = screenVec / (float)ViewModel.Scale * (float)newScale;

        var correctedOffset = new Vector2(
            ViewModel.Offset.X + (screenVec.X - newScreenVec.X),
            ViewModel.Offset.Y + (screenVec.Y - newScreenVec.Y));

        ViewModel.Scale = newScale;
        ViewModel.Offset = correctedOffset;
    }

    private void CanvasPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel == null || !e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed || ViewModel.IsPositionSet || !ViewModel.IsHandEnabled) return;
        ViewModel.StartDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel == null || !e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed || !ViewModel.IsPositionSet || !ViewModel.IsHandEnabled) return;
        ViewModel.UpdateDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ViewModel == null) return;
        ViewModel.IsPositionSet = false;
    }

    private void LoadLayout()
    {
        foreach (var item in Services.Settings.Layout)
        {
            var control = MainLayout.Children.OfType<Control>().FirstOrDefault(c => c.Name == item.Name);
            if (control == null) continue;
            Grid.SetRow(control, item.Row);
            Grid.SetColumn(control, item.Col);
        }

        ApplyGridDefinitions();
        UpdateRectPositions();
    }

    private void ApplyGridDefinitions()
    {
        var verticalItems = MainLayout.Children.OfType<Control>()
            .Where(c => (string?)c.Tag == "Vertical" || c.Name == "CanvasPanel")
            .OrderBy(Grid.GetColumn)
            .ToList();

        for (var i = 0; i < verticalItems.Count && i < MainLayout.ColumnDefinitions.Count; i++)
        {
            MainLayout.ColumnDefinitions[i].Width = verticalItems[i].Name == "CanvasPanel"
                ? new GridLength(1, GridUnitType.Star)
                : GridLength.Auto;
        }

        var horizontalItems = MainLayout.Children.OfType<Control>()
            .Where(c => (string?)c.Tag == "Horizontal" || c.Name == "CanvasPanel")
            .OrderBy(Grid.GetRow)
            .ToList();

        var canvasRow = -1;
        for (var i = 0; i < horizontalItems.Count && i < MainLayout.RowDefinitions.Count; i++)
        {
            if (horizontalItems[i].Name == "CanvasPanel")
            {
                MainLayout.RowDefinitions[i].Height = new GridLength(1, GridUnitType.Star);
                canvasRow = i;
            }
            else
                MainLayout.RowDefinitions[i].Height = GridLength.Auto;
        }

        if (canvasRow >= 0)
        {
            foreach (var child in MainLayout.Children.OfType<Control>().Where(c => (string?)c.Tag == "Vertical"))
            {
                Grid.SetRow(child, canvasRow);
            }
        }

        MainLayout.InvalidateMeasure();
    }

    private void InitializeRects()
    {
        RectHost.Children.Clear();
        _verticalRects = [];
        _horizontalRects = [];

        for (byte i = 0; i <= MainLayout.ColumnDefinitions.Count; i++)
        {
            var rect = new Rectangle { Width = 3 };

            if (i == 0 || i == MainLayout.ColumnDefinitions.Count)
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

            RectHost.Children.Add(rect);
            _verticalRects.Add(rect);
        }

        for (byte i = 0; i <= MainLayout.RowDefinitions.Count; i++)
        {
            var rect = new Rectangle { Height = 3 };

            if (i == 0 || i == MainLayout.RowDefinitions.Count)
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

            RectHost.Children.Add(rect);
            _horizontalRects.Add(rect);
        }

        UpdateRectPositions();
    }

    private void OnMainLayoutLayoutUpdated(object? sender, EventArgs e) => UpdateRectPositions();

    private void UpdateRectPositions()
    {
        double x = 0;
        double y = 0;
        double canvasRowTop = 0;

        var canvasRow = Grid.GetRow(CanvasPanel);

        for (var r = 0; r < canvasRow; r++)
            canvasRowTop += MainLayout.RowDefinitions[r].ActualHeight;

        for (byte i = 0; i <= MainLayout.ColumnDefinitions.Count; i++)
        {
            if (i > 0)
                x += MainLayout.ColumnDefinitions[i - 1].ActualWidth;

            var rect = _verticalRects[i];

            if (i == 0)
                Canvas.SetLeft(rect, x);
            else if (i == MainLayout.ColumnDefinitions.Count)
                Canvas.SetLeft(rect, x - rect.Width);
            else
                Canvas.SetLeft(rect, x - rect.Width / 2);

            Canvas.SetTop(rect, canvasRowTop);
            rect.Height = MainLayout.RowDefinitions[canvasRow].ActualHeight;
        }

        for (byte i = 0; i <= MainLayout.RowDefinitions.Count; i++)
        {
            if (i > 0)
                y += MainLayout.RowDefinitions[i - 1].ActualHeight;

            var rect = _horizontalRects[i];

            if (i == 0)
                Canvas.SetTop(rect, y);
            else if (i == MainLayout.RowDefinitions.Count)
                Canvas.SetTop(rect, y - rect.Height);
            else
                Canvas.SetTop(rect, y - rect.Height / 2);

            Canvas.SetLeft(rect, 0);
            rect.Width = MainLayout.Bounds.Width;
        }
    }

    private static DockState GetDockState(Control panel)
    {
        var parent = panel.Parent as Panel ?? throw new InvalidOperationException();
        return new DockState
        {
            OriginalParent = parent,
            Row = Grid.GetRow(panel),
            Column = Grid.GetColumn(panel),
            Orientation = panel.Tag as string == "Vertical" ? DockOrientation.Vertical : DockOrientation.Horizontal
        };
    }

    private void Undock(Control panel)
    {
        var parent = panel.Parent as Panel ?? throw new InvalidOperationException();
        var pos = panel.TranslatePoint(new Point(0, 0), FloatingHost) ?? default;

        parent.Children.Remove(panel);
        FloatingHost.Children.Add(panel);

        Canvas.SetLeft(panel, pos.X);
        Canvas.SetTop(panel, pos.Y);
    }

    private static void Redock(Control panel, DockState state)
    {
        if (panel.Parent is Panel parent)
            parent.Children.Remove(panel);

        if (!state.OriginalParent.Children.Contains(panel))
            state.OriginalParent.Children.Add(panel);
    }

    private void ReorderElements(Panel parent, Control dragged, byte targetIndex, DockOrientation orientation)
    {
        var items = parent.Children.OfType<Control>()
            .Where(c => c is not null && (MatchesOrientation(c, _dockState!.Orientation) || c.Name == "CanvasPanel"))
            .OrderBy(c => orientation == DockOrientation.Vertical ? Grid.GetColumn(c) : Grid.GetRow(c))
            .ToList();

        var fromIndex = items.IndexOf(dragged);
        items.RemoveAt(fromIndex);

        if (fromIndex < targetIndex) targetIndex--;
        items.Insert(Math.Clamp(targetIndex, 0, items.Count), dragged);

        for (var i = 0; i < items.Count; i++)
        {
            if (orientation == DockOrientation.Vertical)
                Grid.SetColumn(items[i], i);
            else
                Grid.SetRow(items[i], i);
        }

        ApplyGridDefinitions();
    }

    private static bool MatchesOrientation(Control c, DockOrientation orientation)
    {
        return orientation switch
        {
            DockOrientation.Vertical => (string?)c.Tag == "Vertical",
            DockOrientation.Horizontal => (string?)c.Tag == "Horizontal",
            _ => false
        };
    }

    private async void Root_PointerMoved(object? sender, PointerEventArgs e)
    {
        _lastPointerPos = e.GetPosition(TooltipHost);

        var hoveredButton = ToolbarStackPanel.Children.OfType<Control>().FirstOrDefault(b => b.IsPointerOver);
        var tag = hoveredButton?.Tag as string;

        if (tag == _currentTooltipTag) return;

        _currentTooltipTag = tag;
        Tooltip.IsVisible = false;
        _tooltipCts?.Cancel();

        if (tag is null) return;

        _tooltipCts = new CancellationTokenSource();
        var token = _tooltipCts.Token;

        try
        {
            await Task.Delay(500, token);
            if (token.IsCancellationRequested) return;

            Canvas.SetLeft(Tooltip, _lastPointerPos.X + 10);
            Canvas.SetTop(Tooltip, _lastPointerPos.Y + 5);

            TooltipText.Text = string.Concat(tag.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
            Tooltip.IsVisible = true;
        }
        catch (TaskCanceledException) { }
    }

    private async void Panel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control dragged || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        
        _pointerDown = true;
        await Task.Delay(500);
    
        if (!_pointerDown) return;
    
        _dockState ??= GetDockState(dragged);
        Undock(dragged);

        _dragging = true;
    
        var rectsToHighlight = _dockState.Orientation == DockOrientation.Vertical ? _verticalRects : _horizontalRects;

        rectsToHighlight.ForEach(rect => rect.Fill = rect.GetValue(DockInfoProperty)?.Orientation == _dockState.Orientation ?
            new SolidColorBrush(Application.Current?.Resources["PrimaryPressedColor"] as Color? ?? Colors.Blue) : rect.Fill);
    
        e.Pointer.Capture(dragged);
    }

    private void Panel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || sender is not Control dragged) return;

        var pos = e.GetPosition(FloatingHost);
        Canvas.SetLeft(dragged, pos.X);
        Canvas.SetTop(dragged, pos.Y);
    }

    private void Panel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pointerDown = false;
    
        if (sender is not Control dragged || _dockState is null)
        {
            _dragging = false;
            return;
        }

        _dragging = false;
        e.Pointer.Capture(null);

        var docked = false;
        var rectsToCheck = _dockState.Orientation == DockOrientation.Vertical ? _verticalRects : _horizontalRects;

        foreach (var rect in rectsToCheck)
        {
            var dockInfo = rect.GetValue(DockInfoProperty);

            if (dockInfo is null || _dockState is null) continue;

            if (rect.Bounds.Inflate(10).Contains(e.GetPosition(RectHost)))
            {
                Redock(dragged, _dockState);
                ReorderElements(_dockState.OriginalParent, dragged, dockInfo.Index, dockInfo.Orientation);

                Services.Settings.Layout = [.. MainLayout.Children
                    .OfType<Control>()
                    .Select(c => new PanelLayout { Name = c.Name, Row = Grid.GetRow(c), Col = Grid.GetColumn(c) })];

                docked = true;
            }

            var boundaryCount = dockInfo.Orientation == DockOrientation.Vertical
                ? MainLayout.ColumnDefinitions.Count
                : MainLayout.RowDefinitions.Count;

            rect.Fill = (dockInfo.Index == 0 || dockInfo.Index == boundaryCount) ? new SolidColorBrush(Colors.Transparent) : 
                new SolidColorBrush(Application.Current?.Resources["BackgroundPressedColor"] as Color? ?? Colors.DarkGray);
        }

        if (!docked)
        {
            Redock(dragged, _dockState!);
            Grid.SetRow(dragged, _dockState!.Row);
            Grid.SetColumn(dragged, _dockState!.Column);
        }

        UpdateRectPositions();
        _dockState = null;

        Tooltip.IsVisible = false;
        _tooltipCts?.Cancel();
    }
}