using MyAthenaeio.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyAthenaeio.Views
{
    /// <summary>
    /// Interaction logic for RestoreDialog.xaml
    /// </summary>
    public partial class RestoreDialog : Window
    {
        private List<BackupInfo> _availableBackups = new();
        public RestoreDialog()
        {
            InitializeComponent();
            LoadAvailableBackups();
        }

        private void LoadAvailableBackups()
        {
            var backupFiles = Services.BackupService.GetAvailableBackups();
            _availableBackups = [.. backupFiles.Select(f => new BackupInfo
            {
                FilePath = f,
                CreatedDate = File.GetCreationTime(f),
                FileSize = (new FileInfo(f).Length / 1024.0).ToString("F2") + " KB"
            })];

            BackupsListView.ItemsSource = _availableBackups;

            if (_availableBackups.Count == 0)
            {
                BackupsListView.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void BackupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RestoreButton.IsEnabled = BackupsListView.SelectedItem != null;
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (BackupsListView.SelectedItem is BackupInfo selectedBackup)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to restore the library from the backup created on {selectedBackup.DisplayDate}?\n\n" +
                    "This will overwrite your current library data.",
                    "Confirm Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        RestoreButton.IsEnabled = false;
                        CancelButton.IsEnabled = false;

                        await IMEXService.RestoreDatabaseAsync(selectedBackup.FilePath);

                        MessageBox.Show("Library restored successfully. The application will now restart.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Restart the application
                        Process.Start(Environment.ProcessPath!);
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to restore from backup:\n{ex.Message}", "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class BackupInfo
    {
        public string FilePath { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string FileSize { get; set; } = "";

        public string DisplayDate => CreatedDate.ToString("MMMM dd, yyyy 'at' h:mm tt");

        public string RelativeTime
        {
            get
            {
                var span = DateTime.Now - CreatedDate;
                if (span.TotalDays < 1)
                    return $"{(int)span.TotalHours} hours ago";
                if (span.TotalDays < 7)
                    return $"{(int)span.TotalDays} days ago";
                if (span.TotalDays < 30)
                    return $"{(int)(span.TotalDays / 7)} weeks ago";
                return $"{(int)(span.TotalDays / 30)} months ago";
            }
        }
    }
}
