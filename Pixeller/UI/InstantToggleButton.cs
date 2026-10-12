using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Pixeller.UI;

public class InstantToggleButton : ToggleButton
{
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        IsChecked = !IsChecked;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        e.Handled = true;
    }
}