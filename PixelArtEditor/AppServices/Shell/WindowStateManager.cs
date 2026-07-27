using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace PixelArtEditor.AppServices.Shell;

public sealed class WindowStateManager : ReactiveObject
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

                _window.CanResize = value != WindowState.Maximized && value != WindowState.FullScreen;
            }

            PreviousWindowState = _current;
            this.RaiseAndSetIfChanged(ref _current, value);
        }
    }

    public void AttachWindow(Window window)
    {
        _window = window;
        Current = window.WindowState;

        window.GetObservable(Window.WindowStateProperty).Subscribe(state =>
        {
            if (state == _current) return;
            this.RaisePropertyChanged(nameof(Current));

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