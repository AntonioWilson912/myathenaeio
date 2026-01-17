using MyAthenaeio.Services;
using System.Diagnostics;
using System.Windows;

namespace MyAthenaeio.Views
{
    /// <summary>
    /// Interaction logic for ResetDialog.xaml
    /// </summary>
    public partial class ResetDialog : Window
    {
        public ResetDialog()
        {
            InitializeComponent();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                    $"Are you sure you want to reset the library?\n\n" +
                    "This will complete remove all current library data.",
                    "Confirm Reset",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await IMEXService.ResetDatabaseAsync();

                    MessageBox.Show("Library reset successfully. The application will now restart.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Restart the application
                    Process.Start(Environment.ProcessPath!);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to reset database:\n{ex.Message}", "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
