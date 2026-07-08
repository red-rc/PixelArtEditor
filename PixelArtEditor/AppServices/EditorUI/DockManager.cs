using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Dock;
using System;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI;

public static class DockManager
{
    public static void Undock(Control panel, Panel floatingHost)
    {
        panel.Width = panel.Bounds.Width;
        panel.Height = panel.Bounds.Height;

        var parent = panel.FindAncestorOfType<Panel>() ?? throw new InvalidOperationException();
        var pos = panel.TranslatePoint(new Point(0, 0), floatingHost) ?? default;

        parent.Children.Remove(panel);
        floatingHost.Children.Add(panel);

        Avalonia.Controls.Canvas.SetLeft(panel, pos.X);
        Avalonia.Controls.Canvas.SetTop(panel, pos.Y);
    }

    public static void Redock(Control panel, DockState state)
    {
        if (panel.Parent is Panel parent)
            parent.Children.Remove(panel);

        if (!state.OriginalParent.Children.Contains(panel))
            state.OriginalParent.Children.Add(panel);

        panel.ClearValue(Control.WidthProperty);
        panel.ClearValue(Control.HeightProperty);
    }

    public static void ReorderElements(Panel parent, Control dragged, byte targetIndex, DockOrientation orientation, DockState dockState, Action applyGridDefinitions)
    {
        var items = parent.Children.OfType<Control>()
            .Where(c => c is not null && (DockHelper.MatchesOrientation(c, dockState.Orientation) || c.Name == "CanvasPanel"))
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

        applyGridDefinitions();
    }
}
