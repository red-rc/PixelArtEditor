using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.EditorUI;
using PixelArtEditor.AppServices.ImageProcessing;
using PixelArtEditor.ViewModels;
using System.Linq;

namespace PixelArtEditor.Controls.Views;

public partial class StartMenuView : UserControl
{
    private readonly ImageDropHandler _dropHandler;

    public StartMenuView()
    {
        InitializeComponent();

        _dropHandler = new ImageDropHandler(
            o => { (DataContext as StartMenuVM)?.DragBgOpacity = o; },
            v => { (DataContext as StartMenuVM)?.DragImageVisible = v; });

        DragDrop.AddDragOverHandler(this, OnDragOver);
        DragDrop.AddDragLeaveHandler(this, OnDragLeave);
        DragDrop.AddDropHandler(this, OnDrop);
        AddHandler(KeyDownEvent, OnHotkeys, RoutingStrategies.Tunnel);

        AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() => Focus());
    }

    private void OnDragOver(object? sender, DragEventArgs e) => _dropHandler.HandleDragOver(e);
    private async void OnDragLeave(object? sender, RoutedEventArgs e) => await _dropHandler.HandleDragLeave();

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not StartMenuVM vm) return;

        var files = ImageDropHandler.GetFiles(e);
        var file = files.OfType<IStorageFile>().FirstOrDefault();

        vm.DragBgOpacity = 0;
        vm.DragImageVisible = false;

        var model = await ImageImportService.GetPixelModelFromFile(file);
        if (model is null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }

    public async void OnHotkeys(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.Control || e.Key != Key.V) return;

        var model = await ClipboardImageService.GetModel(TopLevel.GetTopLevel(this)?.Clipboard);
        if (model is null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }
}