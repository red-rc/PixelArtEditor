using Avalonia.Input;
using PixelArtEditor.AppServices.Canvas;
using PixelArtEditor.AppServices.EditorUI.LayerPanel;
using PixelArtEditor.ViewModels;
using System;

namespace PixelArtEditor.AppServices.EditorUI;

public class HotkeysService(LayerCmdList commands, LayerManager? layerManager, EditorVM? viewModel, Action onCancel, Action onConfirm)
{
    public bool Handle(KeyModifiers modifiers, Key key)
    {
        switch (modifiers, key)
        {
            case (KeyModifiers.Control, Key.N):
                commands.AddCmd.Execute(layerManager);
                return true;

            case (KeyModifiers.Control, Key.D):
                commands.DuplicateCmd.Execute(layerManager);
                return true;

            case (KeyModifiers.Control, Key.Up):
                commands.MoveStepCmd.Execute(layerManager, -1);
                return true;

            case (KeyModifiers.Control, Key.Down):
                commands.MoveStepCmd.Execute(layerManager, 1);
                return true;

            case (KeyModifiers.Control, Key.G):
                commands.GroupCmd.Execute(layerManager);
                return true;

            case (KeyModifiers.Control, Key.C):
                commands.CopyCmd.Execute(layerManager);
                return true;

            case (KeyModifiers.Control, Key.V):
                commands.InsertCmd.Execute(layerManager);
                return true;

            case (KeyModifiers.None, Key.Delete):
                commands.DeleteCmd.Execute(layerManager);
                return true;

            case (KeyModifiers.None, Key.Escape):
                onCancel();
                return true;

            case (KeyModifiers.None, Key.Enter):
                onConfirm();
                return true;

            case (KeyModifiers.None, Key.B):
                viewModel?.Tools.SelectedTool = Models.Tools.ToolType.Pen;
                return true;

            case (KeyModifiers.None, Key.I):
                viewModel?.Tools.SelectedTool = Models.Tools.ToolType.ColorPicker;
                return true;

            case (KeyModifiers.None, Key.F):
                viewModel?.Tools.SelectedTool = Models.Tools.ToolType.Fill;
                return true;

            case (KeyModifiers.None, Key.E):
                viewModel?.Tools.SelectedTool = Models.Tools.ToolType.Eraser;
                return true;

            case (KeyModifiers.None, Key.H):
                viewModel?.Tools.SelectedTool = Models.Tools.ToolType.Hand;
                return true;

            default:
                return false;
        }
    }
}
