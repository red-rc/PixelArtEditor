using Pixeller.AppServices.Settings;
using Pixeller.AppServices.Shell;

namespace Pixeller.AppServices;

public static class Services
{
    public static NavigationService Navigation { get; set; } = null!;
    public static ISettingsManager Settings { get; set; } = null!;
    public static WindowStateManager WindowState { get; set; } = null!;
}
