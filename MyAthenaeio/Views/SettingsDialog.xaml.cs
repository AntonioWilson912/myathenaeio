using MyAthenaeio.Models.Settings;
using MyAthenaeio.Services;
using System.Windows;

namespace MyAthenaeio.Views
{
    public partial class SettingsDialog : Window
    {
        private readonly SettingsService _settingsService;

        public SettingsDialog(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Settings;

            // Scanner settings
            BackgroundScanningCheckBox.IsChecked = settings.BackgroundScanningEnabled;
            MaxDelayTextBox.Text = settings.MaxKeystrokeDelayMs.ToString();

            // API settings
            TimeoutTextBox.Text = settings.ApiTimeoutSeconds.ToString();

            // Loan settings
            LoanDaysTextBox.Text = settings.DefaultLoanDays.ToString();
            MaxRenewalsTextBox.Text = settings.MaxRenewals.ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate and parse inputs
                if (!int.TryParse(MaxDelayTextBox.Text, out int maxDelay) ||
                    !int.TryParse(TimeoutTextBox.Text, out int timeout) ||
                    !int.TryParse(LoanDaysTextBox.Text, out int loanDays) ||
                    !int.TryParse(MaxRenewalsTextBox.Text, out int maxRenewals))
                {
                    MessageBox.Show("Please ensure all numeric fields contain valid numbers.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Validate ranges
                if (loanDays < 1 || loanDays > 365)
                {
                    MessageBox.Show("Default loan period must be between 1 and 365 days.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (maxRenewals < 0 || maxRenewals > 10)
                {
                    MessageBox.Show("Maximum renewals must be between 0 and 10.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Create new settings
                var newSettings = new AppSettings
                {
                    BackgroundScanningEnabled = BackgroundScanningCheckBox.IsChecked ?? false,
                    MaxKeystrokeDelayMs = maxDelay,
                    ApiTimeoutSeconds = timeout,
                    DefaultLoanDays = loanDays,
                    MaxRenewals = maxRenewals,

                    // Preserve other settings
                    Theme = _settingsService.Settings.Theme,
                    DefaultPageSize = _settingsService.Settings.DefaultPageSize
                };

                _settingsService.UpdateSettings(newSettings);

                MessageBox.Show("Settings saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}