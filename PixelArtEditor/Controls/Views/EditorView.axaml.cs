using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.EditorUI;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.Models.Canvas;
using PixelArtEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PixelArtEditor.Controls.Views;

public partial class EditorView : UserControl
{
    private readonly LayoutManager _layoutManager;
    private readonly TooltipManager _tooltipManager;
    private readonly ImageDropHandler _dropHandler;
    private readonly PanelDragController _dragController;
    private HotkeysService _hotkeysService;

    private IDisposable? _modelSubscription;
    private PixelModel? _subscribedModel;

    private EditorVM? ViewModel => DataContext as EditorVM;

    private readonly List<LayerModel> _addedLayers = [];

    private void OnCancelClick(object? sender, RoutedEventArgs e) => OnCancel();

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => OnConfirm();

    private void OnCancel()
    {
        if (ViewModel is null) return;

        foreach (var layer in _addedLayers)
            ViewModel.LayerManager.Layers.Remove(layer);

        var vm = LayerPanelControl.ViewModel;
        if (LayerPanelControl.LayerManager?.Layers.Count > 0)
        {
            var index = Math.Clamp(1, 0, vm.LayerItems.Count - 1);
            vm.SelLayerItem = vm.LayerItems[index];
        }

        CompleteConfirmation();
    }

    private void OnConfirm()
    {
        CompleteConfirmation();
    }

    private void CompleteConfirmation()
    {
        if (ViewModel is null) return;

        _addedLayers.Clear();
        ViewModel.ConfirmPanelVisible = false;
    }

    public EditorView()
    {
        InitializeComponent();

        _layoutManager = new LayoutManager(MainLayout, RectHost, CanvasPanel);
        _tooltipManager = new TooltipManager(Tooltip, TooltipText, RectHost);

        _dropHandler = new ImageDropHandler(
            o => { ViewModel?.DragBgOpacity = o; },
            v => { ViewModel?.DragImageVisible = v; });

        _dragController = new PanelDragController(MainLayout, FloatingHost, RectHost, _layoutManager);

        _hotkeysService = new HotkeysService(LayerPanelControl.LayerCommands, 
            LayerPanelControl.LayerManager, ViewModel, OnCancel, OnConfirm);

        AddHandler(KeyDownEvent, OnHotkeys, RoutingStrategies.Tunnel);

        DataContextChanged += OnDataContextChanged;

        AttachedToVisualTree += (s, e) =>
        {
            Services.Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(SettingsManager.Layout)) return;
                _layoutManager.LoadLayout();
            };

            _layoutManager.InitializeRects();
            _layoutManager.LoadLayout();

            MainLayout.LayoutUpdated += OnMainLayoutLayoutUpdated;
        };
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
        if (DataContext is not EditorVM vm || vm.IsTransforming) return;
        _dropHandler.HandleDragOver(e);
    }

    private async void OnDragLeave(object? sender, RoutedEventArgs e)
        => await _dropHandler.HandleDragLeave();

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not EditorVM vm || vm.IsTransforming) return;

        var files = ImageDropHandler.GetFiles(e);

        vm.DragBgOpacity = 0;
        vm.DragImageVisible = false;

        foreach (var file in files)
        {
            var pixelModel = await ImageImportService.GetPixelModelFromFile(file);
            if (pixelModel is null) continue;

            var (targetW, targetH) = FitToCanvas(pixelModel.Width, pixelModel.Height, vm.Model.Width, vm.Model.Height);

            var rgba = PixelModelService.ToRgba32(pixelModel);
            var data = BitmapService.SwapRB(rgba);

            if (targetW != pixelModel.Width || targetH != pixelModel.Height) { }
                data = BitmapService.ResizePixelDataScaled(data, pixelModel.Width, pixelModel.Height, targetW, targetH);

            data = BitmapService.CenterOnCanvas(data, targetW, targetH, vm.Model.Width, vm.Model.Height);

            var newLayer = new LayerModel(
                vm.Model.Width,
                vm.Model.Height,
                data,
                pixelModel.Name ?? $"Layer {vm.LayerManager.Layers.Count + 1}"
            );

            vm.LayerManager.Layers.Insert(0, newLayer);

            _addedLayers.Add(newLayer);
        }

        if (_addedLayers.Count <= 0) return;

        LayerPanelControl.LayerListBox.SelectedItems?.Clear();
        foreach (var layer in _addedLayers)
        {
            var item = LayerPanelControl.ViewModel.LayerItems.FirstOrDefault(x => x.Layer == layer);
            if (item is not null) LayerPanelControl.LayerListBox.SelectedItems?.Add(item);
        }

        vm.ConfirmPanelVisible = true;
    }

    private static (int w, int h) FitToCanvas(int srcW, int srcH, int canvasW, int canvasH)
    {
        if (srcW <= canvasW && srcH <= canvasH) return (srcW, srcH);

        var scale = Math.Min((double)canvasW / srcW, (double)canvasH / srcH);
        return (Math.Max(1, (int)(srcW * scale)), Math.Max(1, (int)(srcH * scale)));
    }

    private async void OnHotkeys(object? sender, KeyEventArgs e)
    {
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused is TextBox or NumericUpDown) return;

        if (!_hotkeysService.Handle(e.KeyModifiers, e.Key)) return;

        e.Handled = true;
        Dispatcher.UIThread.Post(() => Root.Focus());
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _modelSubscription?.Dispose();
        _subscribedModel?.ModelChanged -= OnModelChangedHandler;
        _subscribedModel = null;

        if (ViewModel is not null)
        {
            LayerPanelControl.LayerManager = null;

            ViewModel.SetCanvas(CanvasControl);
            LayerPanelControl.LayerManager = ViewModel.LayerManager;
            _hotkeysService = new HotkeysService(LayerPanelControl.LayerCommands, 
                LayerPanelControl.LayerManager, ViewModel, OnCancel, OnConfirm);

            ViewModel.AdjustCanvas(CanvasPanel.Bounds.Width, CanvasPanel.Bounds.Height);

            _modelSubscription = ViewModel.WhenAnyValue(x => x.Model).Subscribe(model =>
            {
                _subscribedModel?.ModelChanged -= OnModelChangedHandler;

                _subscribedModel = model;

                _subscribedModel?.ModelChanged += OnModelChangedHandler;
            });

            Dispatcher.UIThread.Post(() => Root.Focus());
        }

        void OnModelChangedHandler()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (ViewModel is null || CanvasPanel.Bounds is not { Width: > 0, Height: > 0 }) return;
                ViewModel.AdjustCanvas(CanvasPanel.Bounds.Width, CanvasPanel.Bounds.Height);
            });
        }
    }

    private void CanvasPanel_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (ViewModel == null || e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;
        ViewModel.AdjustCanvas(e.NewSize.Width, e.NewSize.Height);

        Dispatcher.UIThread.Post(() => Root.Focus(), DispatcherPriority.Input);
    }

    private void CanvasPanel_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel == null || e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed) return;

        var rawScale = ViewModel.Scale + e.Delta.Y * ViewModel.Scale / 10.0;
        var newScale = Math.Clamp(rawScale, ViewModel.MinScale, ViewModel.MaxScale);

        if (Math.Abs(newScale - ViewModel.Scale) < 1e-9) return;

        var mousePos = e.GetPosition(CanvasPanel);
        var center = new Point(CanvasPanel.Bounds.Width / 2, CanvasPanel.Bounds.Height / 2);

        var screenVec = new Vector2(
            (float)mousePos.X - (float)center.X - ViewModel.Offset.X,
            (float)mousePos.Y - (float)center.Y - ViewModel.Offset.Y);

        var newScreenVec = screenVec / (float)ViewModel.Scale * (float)newScale;

        var correctedOffset = new Vector2(
            ViewModel.Offset.X + (screenVec.X - newScreenVec.X),
            ViewModel.Offset.Y + (screenVec.Y - newScreenVec.Y));

        ViewModel.Scale = newScale;
        ViewModel.Offset = correctedOffset;
    }

    private void CanvasPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            Dispatcher.UIThread.Post(() => Root.Focus());

        if (ViewModel == null
            || !e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed
            || ViewModel.IsPositionSet
            || !ViewModel.IsHandEnabled) return;
        ViewModel.StartDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel == null
            || !e.GetCurrentPoint(CanvasPanel).Properties.IsRightButtonPressed
            || !ViewModel.IsPositionSet
            || !ViewModel.IsHandEnabled) return;
        ViewModel.UpdateDragging(e.GetPosition(CanvasPanel));
    }

    private void CanvasPanel_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ViewModel == null) return;
        ViewModel.IsPositionSet = false;
    }

    private void OnMainLayoutLayoutUpdated(object? sender, EventArgs e) => _layoutManager.UpdateRectPositions();

    private void Root_PointerMoved(object? sender, PointerEventArgs e)
    {
        var buttons = LayerPanelControl.GetVisualDescendants().OfType<Button>()
            .Concat(ToolbarPanel.StackPanel.Children.OfType<Button>());

        _tooltipManager.OnPointerMoved(e, buttons);
    } 

    private async void Panel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || ViewModel == null || ViewModel.IsTransforming) return;
        _dragController.OnPointerPressed(this, e);
    }

    private void Panel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control draggedPanel
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || ViewModel == null
            || ViewModel.IsTransforming) return;

        _dragController.OnPointerMoved(draggedPanel, this, e);
    }

    private void Panel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragController.OnPointerReleased(sender as Control, e);
        _tooltipManager.Hide();
    }
}