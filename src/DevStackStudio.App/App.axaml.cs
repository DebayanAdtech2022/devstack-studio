using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevStackStudio.App.ViewModels;
using DevStackStudio.App.Views;
using DevStackStudio.Core.Contracts;
using DevStackStudio.Generators.Pipeline;
using DevStackStudio.Infrastructure.Docker;
using DevStackStudio.Infrastructure.Environment;
using DevStackStudio.Infrastructure.Process;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DevStackStudio.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: false);
        });
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
        services.AddSingleton<IDockerProbe, DockerProbe>();
        services.AddSingleton<IDockerOrchestrator, DockerOrchestrator>();
        services.AddSingleton<IProjectGenerator, ProjectGenerator>();
        services.AddTransient<MainViewModel>();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
