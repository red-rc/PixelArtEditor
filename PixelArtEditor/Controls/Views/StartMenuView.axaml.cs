using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelArtEditor.Controls.Views;

public partial class StartMenuView : UserControl
{
    private CancellationTokenSource? _leaveCts;

    public StartMenuView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        DragDrop.AddDragOverHandler(this, OnDragOver);
        DragDrop.AddDragLeaveHandler(this, OnDragLeave);
        DragDrop.AddDropHandler(this, OnDrop);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not StartMenuVM vm) return;

        _leaveCts?.Cancel();

        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        vm.DragBgOpacity = e.DragEffects == DragDropEffects.Copy ? 0.3 : 0;
        vm.DragImageVisible = e.DragEffects == DragDropEffects.Copy;
    }

    private async void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StartMenuVM vm) return;

        _leaveCts?.Cancel();
        var cts = new CancellationTokenSource();
        _leaveCts = cts;

        try
        {
            await Task.Delay(30, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        vm.DragBgOpacity = 0;
        vm.DragImageVisible = false;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null) return;

        var file = files.OfType<IStorageFile>().FirstOrDefault();
        if (file is null) return;

        if (DataContext is not StartMenuVM vm) return;
        vm.DragBgOpacity = 0;
        vm.DragImageVisible = false;

        var model = await ImageImportService.GetPixelModelFromFile(file);
        if (model == null) return;

        model.Data = PixelModelService.ToRgba32(model);
        model.Mode = ColorMode.RGBA;
        model.BitDepth = BitDepth.Bit8;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }
}