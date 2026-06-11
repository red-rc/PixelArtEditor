using System;

namespace PixelArtEditor.AppServices.Image;

public class ModelManager
{
    public event Action? ModelChanged;
    public void NotifyModelChanged() => ModelChanged?.Invoke();
}
