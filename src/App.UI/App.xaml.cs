using System.Windows;
using System.IO;
using Prism.Ioc;
using Prism.DryIoc;
using Serilog;
using App.Core;
using App.UI.ViewModels;
using App.UI.Views;
using App.Services;
using App.Infrastructure.Storage;
using App.Infrastructure.Security;
using App.Infrastructure.Updates;
using App.Infrastructure.Http;
using App.Infrastructure.OpenXml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Extensions.Logging;
using Velopack;

namespace App.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class Application : PrismApplication
    {
        protected override Window CreateShell()
        {
            // Initialize Velopack
            VelopackApp.Build()
                .WithFirstRun(v => 
                {
                    System.Windows.MessageBox.Show($"Thanks for installing Doc_Medic v{v}!", 
                        "Installation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                })
                .Run();

            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Configure Serilog
            var logsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Doc_Medic", "logs");
            Directory.CreateDirectory(logsPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    Path.Combine(logsPath, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            containerRegistry.RegisterInstance<Serilog.ILogger>(Log.Logger);

            // Create Microsoft.Extensions.Logging.ILogger adapter for Serilog
            var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);
            containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);
            
            // Register generic logger factory
            containerRegistry.Register(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Logger<>));

            // Register real Core service implementations
            containerRegistry.RegisterSingleton<ISettingsStore, SettingsStore>();
            containerRegistry.RegisterSingleton<ISecretStore, SecretStore>();
            containerRegistry.RegisterSingleton<IUpdater, VelopackUpdater>();

            // Register infrastructure services
            containerRegistry.RegisterSingleton<ILookupService, LookupService>();
            containerRegistry.RegisterSingleton<IHyperlinkIndexService, HyperlinkIndexService>();
            containerRegistry.RegisterSingleton<IHyperlinkRepairService, HyperlinkRepairService>();
            containerRegistry.RegisterSingleton<IFormattingService, FormattingService>();

            // Register main orchestration service
            containerRegistry.RegisterSingleton<IDocumentProcessingService, DocumentProcessingService>();

            // Register ViewModels
            containerRegistry.Register<MainWindowViewModel>();
            containerRegistry.Register<HomePageViewModel>();
            containerRegistry.Register<SettingsPageViewModel>();
            containerRegistry.Register<LogsPageViewModel>();

            // Register Views for navigation
            containerRegistry.RegisterForNavigation<HomePage, HomePageViewModel>();
            containerRegistry.RegisterForNavigation<SettingsPage, SettingsPageViewModel>();
            containerRegistry.RegisterForNavigation<LogsPage, LogsPageViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}

