using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Pixeller.AppServices;
using Pixeller.AppServices.Settings;
using Pixeller.AppServices.Shell;
using Pixeller.ViewModels;
using Pixeller.Windows;

namespace Pixeller;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Do not change the order of these initializations, they depend on each other
        Services.Navigation = new NavigationService();
        Services.WindowState = new WindowStateManager();

        ResourceManager.Initialize();
        var settings = SettingsManager.GetInstance;
        settings.Load();

        Services.Settings = settings;
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowVM()
            };
        }

        base.OnFrameworkInitializationCompleted();

        #if DEBUG
                this.AttachDeveloperTools();
        #endif
    }
}