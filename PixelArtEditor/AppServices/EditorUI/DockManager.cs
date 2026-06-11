using Avalonia;
using Avalonia.Controls;
using PixelArtEditor.Helpers;
using PixelArtEditor.Models.Dock;
using System;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI;

public class DockManager(Panel floatingHost, Action applyGridDefinitions)
{
    private readonly Panel _floatingHost = floatingHost;
    private readonly Action _applyGridDefinitions = applyGridDefinitions;

    public void Undock(Control panel)
    {
        var parent = panel.Parent as Panel ?? throw new InvalidOperationException();
        var pos = panel.TranslatePoint(new Point(0, 0), _floatingHost) ?? default;

        parent.Children.Remove(panel);
        _floatingHost.Children.Add(panel);

        Avalonia.Controls.Canvas.SetLeft(panel, pos.X);
        Avalonia.Controls.Canvas.SetTop(panel, pos.Y);
    }

    public static void Redock(Control panel, DockState state)
    {
        if (panel.Parent is Panel parent)
            parent.Children.Remove(panel);

        if (!state.OriginalParent.Children.Contains(panel))
            state.OriginalParent.Children.Add(panel);
    }

    public void ReorderElements(Panel parent, Control dragged, byte targetIndex, DockOrientation orientation, DockState dockState)
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

        _applyGridDefinitions();
    }
}
