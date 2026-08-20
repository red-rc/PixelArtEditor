using Avalonia.Controls;
using PixelArtEditor.AppServices.EditorUI.LayerPanel.LayerCommands;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.EditorUI.LayerPanel;

public class LayerCmdList(LayerPanelVM vm, ListBox layerListBox)
{
    public CopyCmd CopyCmd { get; set; } = new CopyCmd(vm, layerListBox);
    public InsertCmd InsertCmd { get; set; } = new InsertCmd(vm, layerListBox);
    public AddCmd AddCmd { get; set; } = new AddCmd(vm, layerListBox);
    public RemoveCmd RemoveCmd { get; set; } = new RemoveCmd(vm, layerListBox);
    public DuplicateCmd DuplicateCmd { get; set; } = new DuplicateCmd(vm, layerListBox);
    public GroupCmd GroupCmd { get; set; } = new GroupCmd(vm, layerListBox);
    public MoveCmd MoveCmd { get; set; } = new MoveCmd(vm, layerListBox);
    public MoveStepCmd MoveStepCmd { get; set; } = new MoveStepCmd(vm, layerListBox);
}
