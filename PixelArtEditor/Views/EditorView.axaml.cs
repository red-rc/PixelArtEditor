using System;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.Views;

public partial class EditorView : UserControl
{
    private EditorViewModel ViewModel => DataContext as EditorViewModel ?? 
                                         throw new InvalidOperationException("DataContext is not set");
    public EditorView()
    {
        InitializeComponent();
        
        if (ToolOptionsPanel.ContextMenu is { } toolOptionsPanel)
            toolOptionsPanel.Tag = ToolOptionsPanel;

        if (Toolbar.ContextMenu is { } toolbar)
            toolbar.Tag = Toolbar;

        if (LayerPanel.ContextMenu is { } layerPanel)
            layerPanel.Tag = LayerPanel;
    }
    
    private void CanvasPanel_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Panel panel) return;
        var bounds = panel.Bounds;

        if (bounds is not { Width: > 0, Height: > 0 }) return;
        ViewModel.CalculateScale(bounds.Width, bounds.Height);
    }

    private void CanvasPanel_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var oldScale = ViewModel.Scale;
        var rawScale = oldScale + e.Delta.Y * oldScale / 10.0;
        
        var newScale = Math.Clamp(rawScale, ViewModel.MinScale, ViewModel.MaxScale);
        
        var mousePos = e.GetPosition(CanvasPanel);
        var center = new Point(CanvasPanel.Bounds.Width / 2, CanvasPanel.Bounds.Height / 2);
        
        var screenVec = new Vector2(
            (float)mousePos.X - (float)center.X - ViewModel.Offset.X,
            (float)mousePos.Y - (float)center.Y - ViewModel.Offset.Y);
        
        var newScreenVec = screenVec / (float)oldScale * (float)newScale;
        
        var correctedOffset = new Vector2(
            ViewModel.Offset.X + (screenVec.X - newScreenVec.X),
            ViewModel.Offset.Y + (screenVec.Y - newScreenVec.Y));
        
        ViewModel.Scale = newScale;
        ViewModel.Offset = correctedOffset;
    }
    
    private void CanvasPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed) return;
        if (ViewModel.IsPositionSet) return;
        ViewModel.StartDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed) return;
        if (ViewModel.IsPositionSet == false) return;
        ViewModel.UpdateDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        ViewModel.IsPositionSet = false;
    }

    private void Align_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        if (menuItem.Tag is not string direction) return;
        
        var contextMenu = menuItem.GetLogicalAncestors().OfType<ContextMenu>().FirstOrDefault();
        if (contextMenu?.Tag is not Panel targetPanel) return;

        if (!Enum.TryParse(direction, true, out Dock dock)) return;
        
        DockPanel.SetDock(targetPanel, dock);
        SetBorderAlignment(GetPanelBorder(targetPanel), dock);

        if (targetPanel != Toolbar) return;

        SetToolbarAlignment(dock);
    }

    private void SetToolbarAlignment(Dock dock)
    {
        switch (dock)
        {
            case Dock.Top:
            case Dock.Bottom:
                ToolbarStackPanel.Orientation = Orientation.Horizontal;
                ToolbarStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                ToolbarStackPanel.VerticalAlignment = VerticalAlignment.Center;
                ToolbarStackPanel.Margin = new Thickness(10, 0, 0, 0);
                Toolbar.HorizontalAlignment = HorizontalAlignment.Stretch;
                Toolbar.VerticalAlignment = VerticalAlignment.Top;
                Toolbar.Height = 40;
                Toolbar.Width = double.NaN;
                break;

            case Dock.Left:
            case Dock.Right:
                ToolbarStackPanel.Orientation = Orientation.Vertical;
                ToolbarStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                ToolbarStackPanel.VerticalAlignment = VerticalAlignment.Top;
                ToolbarStackPanel.Margin = new Thickness(0, 10, 0, 0);
                Toolbar.VerticalAlignment = VerticalAlignment.Stretch;
                Toolbar.HorizontalAlignment = HorizontalAlignment.Left;
                Toolbar.Width = 40;
                Toolbar.Height = double.NaN;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void BringToFront(object? _, RoutedEventArgs __) => MoveToolbar(RootPanel.Children.Count - 2);
    private void BringToBack(object? _, RoutedEventArgs __) => MoveToolbar(1);

    private void MoveToolbar(int index)
    {
        RootPanel.Children.Remove(Toolbar);
        RootPanel.Children.Insert(index, Toolbar);
    }
    
    private Rectangle GetPanelBorder(Panel panel)
    {
        return panel.Name switch
        {
            nameof(ToolOptionsPanel) => ToolOptionsPanelBorder,
            nameof(Toolbar) => ToolbarBorder,
            nameof(LayerPanel) => LayerPanelBorder,
            _ => throw new InvalidOperationException("Unknown panel")
        };
    }

    private static void SetBorderAlignment(Rectangle rect, Dock dock)
    {
        switch (dock)
        {
            case Dock.Top:
                rect.VerticalAlignment = VerticalAlignment.Bottom;
                rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                rect.Height = 3;
                rect.Width = double.NaN;
                break;
            case Dock.Bottom:
                rect.VerticalAlignment = VerticalAlignment.Top;
                rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                rect.Height = 3;
                rect.Width = double.NaN;
                break;
            case Dock.Left:
                rect.HorizontalAlignment = HorizontalAlignment.Right;
                rect.VerticalAlignment = VerticalAlignment.Stretch;
                rect.Width = 3;
                rect.Height = double.NaN;
                break;
            case Dock.Right:
                rect.HorizontalAlignment = HorizontalAlignment.Left;
                rect.VerticalAlignment = VerticalAlignment.Stretch;
                rect.Width = 3;
                rect.Height = double.NaN;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dock), dock, null);
        }
    }
}