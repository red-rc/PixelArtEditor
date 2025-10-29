using System.Threading.Tasks;

namespace PixelArtEditor.AppServices;

public interface IActionsService
{
    Task ShowCreateWindowAsync();
    Task ShowExportWindowAsync();
    Task ShowSettingsWindowAsync();
}