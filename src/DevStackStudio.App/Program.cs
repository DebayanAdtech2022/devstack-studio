using Avalonia;
using DevStackStudio.App;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevStackStudio", "logs", "app-.log"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    Log.Fatal(e.ExceptionObject as Exception, "AppDomain Unhandled Exception");
    Log.CloseAndFlush();
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    Log.Error(e.Exception, "TaskScheduler UnobservedTaskException");
    e.SetObserved();
};

try
{
    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application fatal crash in Main");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
