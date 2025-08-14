using System.Threading.Tasks;

namespace PixelArtEditor.Services;

public interface IActionsService
{
    Task ShowCreateWindowAsync(IAppServices services);
    Task ShowSettingsWindowAsync(IAppServices services);
}