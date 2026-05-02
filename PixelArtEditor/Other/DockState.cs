using Avalonia.Controls;

namespace PixelArtEditor.Other;

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

