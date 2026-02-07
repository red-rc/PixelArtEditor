using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using PixelArtEditor.Other;
using PixelArtEditor.ViewModels;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace PixelArtEditor.Views;

public partial class EditorView : UserControl
{
    private bool _dragging;
    private DockState? _dockState;

    private EditorVM? ViewModel => DataContext as EditorVM;

    public EditorView()
    {
        InitializeComponent();

        InitializeComponent();
        AttachedToVisualTree += (s, e) =>
        {
            if (DataContext is EditorVM vm)
            {
                vm.SetCanvas(CanvasControl);
            }
        };
    }

    private void CanvasPanel_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (ViewModel == null || e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;
        ViewModel.AdjustCanvas(e.NewSize.Width, e.NewSize.Height);
    }

    private void CanvasPanel_DataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel == null || sender is not Panel panel || panel.Bounds is not { Width: > 0, Height: > 0 }) return;
        ViewModel.AdjustCanvas(panel.Bounds.Width, panel.Bounds.Height);
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

    private static DockState GetDockState(Control panel)
    {
        var parent = panel.Parent as Panel ?? throw new InvalidOperationException();
        return new DockState
        {
            OriginalParent = parent,
            Row = Grid.GetRow(panel),
            Column = Grid.GetColumn(panel)
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

    private void Redock(Control panel, DockState state)
    {
        FloatingHost.Children.Remove(panel);
        state.OriginalParent.Children.Add(panel);
    }

    private void ReorderColumns(Panel parent, Control dragged, int targetRow, int targetColumn)
    {
        Grid.SetRow(dragged, targetRow);

        var items = parent.Children.OfType<Control>().OrderBy(Grid.GetColumn).ToList();
        var from = items.IndexOf(dragged);

        if (from == targetColumn) return;

        items.RemoveAt(from);
        items.Insert(targetColumn, dragged);

        while (MainLayout.ColumnDefinitions.Count < items.Count)
        {
            MainLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }

        for (var i = 0; i < items.Count; i++)
        {
            MainLayout.ColumnDefinitions[i].Width = items[i].Name == "CanvasPanel" ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
            Grid.SetColumn(items[i], i);
        }
    }

    private async void Panel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control dragged || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        await Task.Delay(500);

        _dockState ??= GetDockState(dragged);
        Undock(dragged);

        _dragging = true;

        var panelTags = dragged.GetLogicalDescendants().OfType<Rectangle>().Select(rect => rect.Tag).ToHashSet();
        var rects = MainLayout.GetLogicalDescendants().OfType<Rectangle>().Where(rect => panelTags.Contains(rect.Tag)).ToList();

        foreach (Rectangle rect in rects)
        {
            if (this.TryFindResource("PrimaryPressedColor", out var colorObj) && colorObj is Color color)
            {
                rect.Fill = new SolidColorBrush(color);
            }
        }

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
        if (sender is not Control dragged || _dockState is null) return;

        _dragging = false;
        e.Pointer.Capture(null);

        var panelTags = dragged.GetLogicalDescendants().OfType<Rectangle>().Select(rect => rect.Tag).ToHashSet();
        var rects = MainLayout.GetLogicalDescendants().OfType<Rectangle>().Where(rect => panelTags.Contains(rect.Tag)).ToList();

        var docked = false;

        foreach (var rect in rects)
        {
            if (rect.Parent is not Panel targetPanel || _dockState is null) continue;

            if (rect.Bounds.Inflate(10).Contains(e.GetPosition(targetPanel)))
            {
                if (targetPanel.Parent is Panel parent)
                {
                    Redock(dragged, _dockState);
                    ReorderColumns(parent, dragged, Grid.GetRow(targetPanel), Grid.GetColumn(targetPanel));

                    docked = true;
                }
            }

            if (this.TryFindResource("BackgroundPressedColor", out var colorObj) && colorObj is Color color)
            {
                rect.Fill = new SolidColorBrush(color);
            }
        }

        if (!docked)
        {
            Redock(dragged, _dockState!);
            Grid.SetRow(dragged, _dockState!.Row);
            Grid.SetColumn(dragged, _dockState!.Column);
        }

        _dockState = null;
    }
}