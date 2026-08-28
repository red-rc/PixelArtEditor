using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace PixelArtEditor.AppServices.Shell;

public sealed class WindowStateManager : ReactiveObject
{
    private Window? _window;
    public WindowState PrevWindowState;

    private WindowState _current;
    public WindowState Current
    {
        get => _current;
        set
        {
            if (Dispatcher.UIThread.CheckAccess())
                SetCurrent(value);
            else
                Dispatcher.UIThread.Post(() => SetCurrent(value));
        }
    }

    private void SetCurrent(WindowState state)
    {
        if (_current == state) return;

        PrevWindowState = _current;
        this.RaiseAndSetIfChanged(ref _current, state, nameof(Current));

        if (_window != null)
        {
            _window.WindowState = state;
            _window.CanResize = state != WindowState.Maximized && state != WindowState.FullScreen;
        }
    }

    public void AttachWindow(Window window)
    {
        _window = window;
        Current = window.WindowState;

        window.GetObservable(Window.WindowStateProperty).Subscribe(state =>
        {
            _current = state;
        });
    }
}