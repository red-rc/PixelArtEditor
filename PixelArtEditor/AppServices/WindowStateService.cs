using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;

namespace PixelArtEditor.AppServices;

public sealed class WindowStateService : ReactiveObject
{
    private WindowState _current;
    private Window? _window;
    public WindowState PreviousWindowState;

    public WindowState Current
    {
        get => _current;
        set
        {
            if (_current == value) return;

            if (_window != null)
            {
                if (Dispatcher.UIThread.CheckAccess()) 
                    _window.WindowState = value;
                else 
                    Dispatcher.UIThread.Post(() => _window.WindowState = value);

                if (value == WindowState.Maximized || value == WindowState.FullScreen)
                {
                    _window.ExtendClientAreaToDecorationsHint = false;
                    _window.CanResize = false;
                }
                else
                {
                    _window.ExtendClientAreaToDecorationsHint = true;
                    _window.CanResize = true;
                }
            }

            PreviousWindowState = _current;
            this.RaiseAndSetIfChanged(ref _current, value);
        }
    }

    public void AttachWindow(Window window)
    {
        _window = window;
        Current = window.WindowState;
    }
}