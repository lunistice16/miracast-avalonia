using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MiraQt.Services;
using MiraQt.ViewModels;
using MiraQt.Views;

namespace MiraQt;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            INetworkDisplaysService service;
            
            // WindowsではD-Busに繋がらないのでモックを使ってUIテストできるようにする
            if (System.OperatingSystem.IsWindows())
            {
                service = new MockNetworkDisplaysService();
            }
            else
            {
                service = new NetworkDisplaysService();
            }

            var vm = new MainViewModel(service);

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };

            desktop.Exit += (_, _) => service.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
