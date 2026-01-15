using MyAthenaeio.Data;
using Application = System.Windows.Application;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using System.Threading.Tasks;
using MyAthenaeio.Services;

namespace MyAthenaeio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
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
                MessageBox.Show(
                    $"Failed to initialize database:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }
    }
}
