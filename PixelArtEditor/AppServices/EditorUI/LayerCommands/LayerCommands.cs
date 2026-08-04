using Avalonia.Controls;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.AppServices.EditorUI.LayerCommands;

public class LayerCommands(LayerPanelVM vm, ListBox layerListBox)
{
    public CopyCommand CopyCommand { get; set; } = new CopyCommand(vm, layerListBox);
    public DuplicateCommand DuplicateCommand { get; set; } = new DuplicateCommand(vm, layerListBox);
    public InsertCommand InsertCommand { get; set; } = new InsertCommand(vm, layerListBox);
    public MoveCommand MoveCommand { get; set; } = new MoveCommand(vm, layerListBox);
    public MoveStepCommand MoveStepCommand { get; set; } = new MoveStepCommand(vm, layerListBox);
}
