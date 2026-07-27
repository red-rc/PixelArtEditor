using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PixelArtEditor.AppServices;
using PixelArtEditor.AppServices.Image;
using PixelArtEditor.AppServices.Shell;
using PixelArtEditor.ViewModels;
using PixelArtEditor.Windows;

namespace PixelArtEditor;

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
        Services.ModelData = new ModelManager();
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