using AutoPartsPOS.Application;
using AutoPartsPOS.Infrastructure;
using AutoPartsPOS.Persistence;
using AutoPartsPOS.WPF.Services;
using AutoPartsPOS.WPF.ViewModels;
using AutoPartsPOS.WPF.Backups.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace AutoPartsPOS.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureGlobalExceptionHandling();

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureAppConfiguration(configuration =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                configuration.AddEnvironmentVariables(prefix: "AUTOPARTSPOS_");
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure();
                services.AddPersistence(context.Configuration);
                services.AddPresentation();
            })
            .Build();

        await _host.StartAsync();

        await InitializeDatabaseAsync();

        try
        {
            var backupService = _host.Services.GetRequiredService<IDataBackupService>();
            await backupService.EnsureDailyBackupAsync();
        }
        catch (Exception exception)
        {
            var startupLog = Path.Combine(AppContext.BaseDirectory, "Logs", "startup.log");
            WriteStartupLog(startupLog, $"Daily backup failed; application startup will continue.{Environment.NewLine}{exception}");
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        var shellViewModel = _host.Services.GetRequiredService<ShellViewModel>();
        await shellViewModel.InitializeAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        var backupsDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(logDirectory);
        Directory.CreateDirectory(backupsDirectory);
        var startupLog = Path.Combine(logDirectory, "startup.log");
        var databasePath = Path.Combine(dataDirectory, "Database.db");
        var databaseExisted = File.Exists(databasePath);

        try
        {
            WriteStartupLog(startupLog, $"Application startup. Provider=SQLite; Database={databasePath}; DatabaseExists={databaseExisted}.");
            WriteStartupLog(startupLog, "Executing database migrations and required settings seed.");
            var initializer = _host!.Services.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync();
            WriteStartupLog(startupLog, $"Database initialization completed successfully. DatabaseCreated={!databaseExisted && File.Exists(databasePath)}.");
        }
        catch (Exception exception)
        {
            WriteStartupLog(startupLog, $"Fatal database initialization error.{Environment.NewLine}{exception}");
            throw;
        }
    }

    private static void WriteStartupLog(string path, string message)
    {
        File.AppendAllText(path, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void ConfigureGlobalExceptionHandling()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            LogUnhandledException(args.ExceptionObject as Exception, "AppDomain unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogUnhandledException(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogUnhandledException(e.Exception, "Dispatcher unhandled exception");
        MessageBox.Show(
            "حدث خطأ غير متوقع. الرجاء إغلاق التطبيق وإعادة المحاولة.",
            "خطأ",
            MessageBoxButton.OK,
            MessageBoxImage.Error,
            MessageBoxResult.OK,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

        e.Handled = true;
    }

    private void LogUnhandledException(Exception? exception, string message)
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(
                Path.Combine(logDirectory, "runtime-errors.log"),
                $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
        }

        if (_host is null)
        {
            return;
        }

        var logger = _host.Services.GetService<ILogger<App>>();
        logger?.LogCritical(exception, "{Message}", message);
    }
}
