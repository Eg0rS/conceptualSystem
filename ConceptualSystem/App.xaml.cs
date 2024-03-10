using System.Windows;
using ConceptualSystem.Menu;
using Injection;
using Injection.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConceptualSystem;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost host;

    public App()
    {
        host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.RegisterAllDependency<IDependency>(typeof(App).Assembly);
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        host.Start();
        var s = host.Services.GetRequiredService<MenuWindow>();
        MainWindow = host.Services.GetRequiredService<MenuWindow>();
        MainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        host.StopAsync();
        host.Dispose();

        base.OnExit(e);
    }
}
