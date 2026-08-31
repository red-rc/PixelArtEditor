using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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

            _tooltipText.Text = tag;

            var formatted = new FormattedText(
                _tooltipText.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                13,
                Brushes.Black);

            var textWidth = formatted.Width;
            var right = _lastPointerPos.X > _tooltipHost.Bounds.Width - (textWidth + 25)
                ? _lastPointerPos.X - (textWidth + 10)
                : _lastPointerPos.X + 5;

            var top = _lastPointerPos.Y > _tooltipHost.Bounds.Height - 35
                ? _lastPointerPos.Y - 30
                : _lastPointerPos.Y + 5;

            Avalonia.Controls.Canvas.SetLeft(_tooltipControl, right);
            Avalonia.Controls.Canvas.SetTop(_tooltipControl, top);

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
