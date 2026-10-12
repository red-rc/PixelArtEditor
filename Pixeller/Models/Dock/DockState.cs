using Avalonia.Controls;

namespace Pixeller.Models.Dock;

public enum DockOrientation
{
    Horizontal,
    Vertical
}

public class DockState
{
    public Panel OriginalParent = null!;
    public int Row;
    public int Column;
    public DockOrientation Orientation;
}

