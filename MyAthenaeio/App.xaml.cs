using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Data;
using MyAthenaeio.Services;
using Serilog;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MyAthenaeio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Configure Serilog
            ConfigureLogging();

            Log.Information("myAthenaeio v{Version} starting",
                Assembly.GetExecutingAssembly().GetName().Version);
            Log.Information("Database location: {DbPath}", AppDbContext.DbPath);


            base.OnStartup(e);

            // Initialize database on app startup
            try
            {
                DatabaseInitializer.Initialize();

                // Check and create automatic backup if needed
                await BackupService.CheckAndCreateAutomaticBackupAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to initialize database");
                MessageBox.Show(
                    $"Failed to initialize database:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
                return;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shuttong down with exit code {ExitCode}", e.ApplicationExitCode);
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static void ConfigureLogging()
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "myAthenaeio",
                "logs",
                "app.log"
            );

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
        }
    }
}
