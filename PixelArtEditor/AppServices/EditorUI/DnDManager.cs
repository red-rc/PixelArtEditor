using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.Models.LayerPanel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArtEditor.AppServices.EditorUI;

public class DnDManager(ListBox layerListBox, Avalonia.Controls.Canvas floatingHost,
    Border countBadge, TextBlock countBadgeText)
{
    public LayerManager? LayerManager { get; set; }

    private readonly ListBox LayerListBox = layerListBox;
    private readonly Avalonia.Controls.Canvas FloatingHost = floatingHost;
    private readonly Border CountBadge = countBadge;
    private readonly TextBlock CountBadgeText = countBadgeText;

    public int ItemHeight { get; set; }
    public int? TargetIndex { get; set; }

    public List<ListBoxItem> DraggedItems { get; set; } = [];
    private ScrollViewer? _scrollViewer;

    private IEnumerable<LayerItem> DraggedLayerItems =>
        DraggedItems.Select(d => d.DataContext).OfType<LayerItem>();
    private ScrollViewer? GetScrollViewer() =>
        _scrollViewer ??= LayerListBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

    public void StartDragVisual()
    {
        foreach (var item in DraggedItems)
            item.Opacity = 0;

        var stackCount = DraggedItems.Count > 3 ? 1 : DraggedItems.Count;

        for (var i = 0; i < stackCount; i++)
        {
            var item = DraggedItems[i].DataContext as LayerItem;

            var preview = new ContentPresenter
            {
                Content = item,
                ContentTemplate = LayerListBox.ItemTemplate,
                Opacity = 0.85,
                ZIndex = -i
            };

            FloatingHost.Children.Add(preview);
        }

        CollapseGap();
    }

    private void CollapseGap()
    {
        if (DraggedItems.Count <= 3) return;

        var firstDraggedIndex = LayerListBox.Items.IndexOf(DraggedLayerItems.FirstOrDefault());

        for (var i = firstDraggedIndex + 1; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.Items[i] is not LayerItem item || DraggedLayerItems.Any(li => li.Layer == item.Layer)) continue;

            if (LayerListBox.ContainerFromIndex(i) is ListBoxItem listBoxItem)
                listBoxItem.RenderTransform = TransformOperations.Parse($"translateY(-{(DraggedItems.Count - 1) * ItemHeight}px)");
        }
    }

    public void AutoScrollIfNeeded(PointerEventArgs e)
    {
        var scrollViewer = GetScrollViewer();
        if (scrollViewer is null) return;

        var pos = e.GetPosition(LayerListBox);
        const double edge = 20;
        const double speed = 5;

        if (pos.Y < edge)
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, scrollViewer.Offset.Y - speed));
        else if (pos.Y > LayerListBox.Bounds.Height - edge)
        {
            var offsetY = Math.Min(scrollViewer.Extent.Height - scrollViewer.Viewport.Height, scrollViewer.Offset.Y + speed);
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, offsetY);
        }
    }

    public int GetTargetIndex(PointerEventArgs e)
    {
        if (ItemHeight <= 0) return 0;

        var scrollOffset = GetScrollViewer()?.Offset.Y ?? 0;
        var y = e.GetPosition(LayerListBox).Y + scrollOffset;

        var baseGap = DraggedItems.Count > 3 ? (DraggedItems.Count - 1) * ItemHeight : 0;

        var sourceNonSelectedIndex = 0;
        foreach (var layer in LayerManager!.Layers)
        {
            if (DraggedLayerItems.Any(li => li.Layer == layer)) break;
            sourceNonSelectedIndex++;
        }

        var nonSelectedIndex = 0;
        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.Items[i] is not LayerItem item || DraggedItems.Any(d => d.DataContext == item)) continue;

            var itemTop = i * ItemHeight;
            if (nonSelectedIndex >= sourceNonSelectedIndex)
                itemTop -= baseGap;

            if (y < itemTop + ItemHeight / 2.0)
                return nonSelectedIndex;

            nonSelectedIndex++;
        }

        return nonSelectedIndex;
    }

    public void AnimateItems()
    {
        if (TargetIndex is null) return;

        var stackCount = DraggedItems.Count > 3 ? 1 : DraggedItems.Count;
        var baseGap = DraggedItems.Count > 3 ? (DraggedItems.Count - 1) * ItemHeight : 0;

        var sourceNonSelectedIndex = 0;
        foreach (var layer in LayerManager!.Layers)
        {
            if (DraggedLayerItems.Any(li => li.Layer == layer)) break;
            sourceNonSelectedIndex++;
        }

        var nonSelectedIndex = 0;

        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.Items[i] is not LayerItem item || DraggedLayerItems.Any(li => li.Layer == item.Layer)) continue;

            if (LayerListBox.ContainerFromIndex(i) is ListBoxItem listBoxItem)
            {
                double targetY = nonSelectedIndex >= sourceNonSelectedIndex ? -baseGap : 0;

                if (TargetIndex > sourceNonSelectedIndex && nonSelectedIndex >= sourceNonSelectedIndex
                    && nonSelectedIndex < TargetIndex)
                    targetY = -baseGap - stackCount * ItemHeight;
                else if (TargetIndex < sourceNonSelectedIndex && nonSelectedIndex < sourceNonSelectedIndex
                    && nonSelectedIndex >= TargetIndex)
                    targetY = stackCount * ItemHeight;

                listBoxItem.RenderTransform = TransformOperations.Parse($"translateY({targetY}px)");
            }

            nonSelectedIndex++;
        }
    }

    public void MoveGroupTo(int targetIndex)
    {
        var group = DraggedLayerItems.ToList();
        var layers = LayerManager!.Layers;

        var withoutGroup = layers.Where(l => !group.Any(g => g.Layer == l)).ToList();
        withoutGroup.InsertRange(Math.Clamp(targetIndex, 0, withoutGroup.Count), group.Select(g => g.Layer));

        for (var i = 0; i < withoutGroup.Count; i++)
        {
            var currentIndex = layers.IndexOf(withoutGroup[i]);
            if (currentIndex != i)
                layers.Move(currentIndex, i);
        }

        RestoreSelectionFor();
    }

    private void RestoreSelectionFor()
    {
        LayerListBox.SelectedItems?.Clear();

        foreach (var item in DraggedLayerItems)
            LayerListBox.SelectedItems?.Add(item);
    }

    public void CleanupDrag()
    {
        FloatingHost.Children.Clear();

        foreach (var item in DraggedItems)
            item.Opacity = 1;

        DraggedItems.Clear();
        TargetIndex = null;

        if (CountBadge.IsVisible)
        {
            CountBadge.IsVisible = false;
            CountBadgeText.Text = "0";
        }
    }

    public void ResetItemsTransform()
    {
        for (var i = 0; i < LayerListBox.ItemCount; i++)
        {
            if (LayerListBox.ContainerFromIndex(i) is ListBoxItem listboxItem)
            {
                var transitions = listboxItem.Transitions;
                listboxItem.Transitions = null;
                listboxItem.RenderTransform = TransformOperations.Identity;
                listboxItem.Transitions = transitions;
            }
        }
    }
}
