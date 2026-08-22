using Avalonia.Input;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.AppServices.EditorUI;

public class ImageDropHandler(Action<double> setOpacity, Action<bool> setImageVisible)
{
    private CancellationTokenSource? _leaveCts;

    public bool HandleDragOver(DragEventArgs e)
    {
        _leaveCts?.Cancel();

        var effect = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.DragEffects = effect;
        var isCopy = effect == DragDropEffects.Copy;

        setOpacity(isCopy ? 0.3 : 0);
        setImageVisible(isCopy);

        return isCopy;
    }

    public async Task HandleDragLeave()
    {
        _leaveCts?.Cancel();
        var cts = new CancellationTokenSource();
        _leaveCts = cts;

        try { await Task.Delay(30, cts.Token); }
        catch (TaskCanceledException) { return; }

        setOpacity(0);
        setImageVisible(false);
    }

    public static IEnumerable<IStorageFile> GetFiles(DragEventArgs e)
        => e.DataTransfer.TryGetFiles()?.OfType<IStorageFile>() ?? [];
}
