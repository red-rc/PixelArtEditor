using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.EditorUI;

public class TooltipManager(Control tooltipControl, TextBlock tooltipText, Panel tooltipHost)
{
    private readonly Control _tooltipControl = tooltipControl;
    private readonly TextBlock _tooltipText = tooltipText;
    private readonly Visual _tooltipHost = tooltipHost;

    private string? _currentTooltipTag;
    private Point _lastPointerPos;
    private CancellationTokenSource? _tooltipCts;

    public async void OnPointerMoved(PointerEventArgs e, IEnumerable<Control> buttons) 
    {
        _lastPointerPos = e.GetPosition(_tooltipHost);

        var hoveredButton = buttons.FirstOrDefault(b => b.IsPointerOver);
        var tag = hoveredButton?.Tag as string;

        if (tag == _currentTooltipTag) return;

        _currentTooltipTag = tag;
        _tooltipControl.IsVisible = false;
        _tooltipCts?.Cancel();

        if (tag is null) return;

        _tooltipCts = new CancellationTokenSource();
        var token = _tooltipCts.Token;

        try
        {
            await Task.Delay(500, token);
            if (token.IsCancellationRequested) return;

            Avalonia.Controls.Canvas.SetLeft(_tooltipControl, _lastPointerPos.X + 10);
            Avalonia.Controls.Canvas.SetTop(_tooltipControl, _lastPointerPos.Y + 5);

            _tooltipText.Text = string.Concat(tag.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
            _tooltipControl.IsVisible = true;
        }
        catch (TaskCanceledException) { }
    }

    public void Hide()
    {
        _tooltipControl.IsVisible = false;
        _tooltipCts?.Cancel();
    }
}
