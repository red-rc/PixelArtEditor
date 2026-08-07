using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PixelArtEditor.Controls.Views;

public partial class StartMenuView : UserControl
{
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
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        if (DataContext is not StartMenuVM vm) return;
        vm.CanvasOpacity = e.DragEffects == DragDropEffects.Copy ? 0.3 : 0;
        vm.ImageVisible = e.DragEffects == DragDropEffects.Copy;
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StartMenuVM vm) return;
        vm.CanvasOpacity = 0;
        vm.ImageVisible = false;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null) return;

        var file = files.OfType<IStorageFile>().FirstOrDefault();
        if (file is null) return;

        if (DataContext is not StartMenuVM vm) return;
        vm.CanvasOpacity = 0;
        vm.ImageVisible = false;

        var model = await ImageImportService.GetPixelModelFromFile(file);
        if (model == null) return;

        Services.Navigation.NavigateTo(new EditorVM(model));
    }
}