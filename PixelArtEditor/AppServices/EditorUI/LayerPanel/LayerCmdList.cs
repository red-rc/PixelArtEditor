using Avalonia.Controls;
using PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel;

public class LayerCmdList(LayerPanelVM vm, ListBox layerListBox, TopLevel topLevel)
{
    public CopyCmd CopyCmd { get; set; } = new CopyCmd(vm, layerListBox, topLevel);
    public PasteCmd PasteCmd { get; set; } = new PasteCmd(vm, layerListBox, topLevel);
    public AddCmd AddCmd { get; set; } = new AddCmd(vm, layerListBox, topLevel);
    public DeleteCmd DeleteCmd { get; set; } = new DeleteCmd(vm, layerListBox, topLevel);
    public DuplicateCmd DuplicateCmd { get; set; } = new DuplicateCmd(vm, layerListBox, topLevel);
    public GroupCmd GroupCmd { get; set; } = new GroupCmd(vm, layerListBox, topLevel);
    public MoveCmd MoveCmd { get; set; } = new MoveCmd(vm, layerListBox, topLevel);
    public MoveStepCmd MoveStepCmd { get; set; } = new MoveStepCmd(vm, layerListBox, topLevel);
}
