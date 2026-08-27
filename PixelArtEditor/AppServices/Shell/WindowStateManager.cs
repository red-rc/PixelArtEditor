using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace PixelArtEditor.AppServices.Shell;

public sealed class WindowStateManager : ReactiveObject
{
    private Window? _window;
    public WindowState PreviousWindowState;

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

        PreviousWindowState = _current;
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
            if (Current != state)
                Current = state;

            if (state == WindowState.Normal)
                ForceRelayout();
        });
    }

    private void ForceRelayout()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_window is null) return;

            _window.ClearValue(Window.WidthProperty);
            _window.ClearValue(Window.HeightProperty);

            _window.InvalidateMeasure();
            _window.InvalidateArrange();
            _window.InvalidateVisual();

            if (_window.Content is Control content)
            {
                content.InvalidateMeasure();
                content.InvalidateArrange();
            }

            Dispatcher.UIThread.Post(() =>
            {
                var width = _window.Bounds.Width;
                var height = _window.Bounds.Height;

                _window.Width = width + 1;
                _window.Height = height;

                Dispatcher.UIThread.Post(() =>
                {
                    _window.Width = width;
                    _window.Height = height;
                }, DispatcherPriority.Background);
            }, DispatcherPriority.Background);

        }, DispatcherPriority.Background);
    }
}