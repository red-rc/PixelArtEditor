using Avalonia.Controls;
using PixelArtEditor.Models.Dock;
using System;

namespace PixelArtEditor.Helpers;

public static class DockHelper
{
    public static DockState GetDockState(Control panel)
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

    public static bool MatchesOrientation(Control c, DockOrientation orientation)
    {
        return orientation switch
        {
            DockOrientation.Vertical => (string?)c.Tag == "Vertical",
            DockOrientation.Horizontal => (string?)c.Tag == "Horizontal",
            _ => false
        };
    }
}
