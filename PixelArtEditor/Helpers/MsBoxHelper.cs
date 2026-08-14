using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace PixelArtEditor.Helpers;

public static class MsBoxHelper
{
    public static async Task ShowErrorAsync(string message, string name = "Error")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(name, $"Failed to open the file: {message}", ButtonEnum.Ok, Icon.Error);
        await box.ShowAsync();
    }
}
