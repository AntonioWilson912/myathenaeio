using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MyAthenaeio.Scanner;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using Brushes = System.Windows.Media.Brushes;

namespace MyAthenaeio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScannerManager _scannerManager;
        private TrayIconManager _trayIconManager;
        private ObservableCollection<ScanLogEntry> _scanLog;
        private int _scanCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            _scanLog = new ObservableCollection<ScanLogEntry>();
            ScanLogList.ItemsSource = _scanLog;

            _scannerManager = new ScannerManager();
            _scannerManager.BarcodeScanned += OnBarcodeScanned;

            // Initialize system tray
            _trayIconManager = new TrayIconManager(_scannerManager);

            // Set initial mode when window loads
            Loaded += (s, e) =>
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                UpdateCurrentModeText();
            };

            // Handle minimize to tray behavior
            StateChanged += Window_StateChanged;
        }

        private void ScannerInputField_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Handle manual input
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                string text = ScannerInputField.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    ProcessManualInput(text);
                    e.Handled = true;
                }
            }
            else
            {
                // Pass key to scanner manager
                _scannerManager.ProcessKey(e.Key);
            }
        }

        private void ProcessManualInput(string input)
        {
            // Validate ISBN
            if (ISBNValidator.IsValidISBNFormat(input))
            {
                // Clean and process
                string cleaned = ISBNValidator.CleanISBN(input);
                OnBarcodeScanned(this, cleaned);
            }
            else
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Text = $"❌ Invalid ISBN format: {input}";
            }
        }


        private void OnBarcodeScanned(object sender, string barcode)
        {
            Dispatcher.Invoke(() =>
            {
                _scanCount++;

                // Add to log
                _scanLog.Insert(0, new ScanLogEntry
                {
                    Timestamp = DateTime.Now,
                    Barcode = ISBNValidator.FormatISBN(barcode),
                    Source = sender == this ? "Manual" : (IsActive ? "Scanner": "Background")
                });

                // Update UI
                StatusText.Foreground = Brushes.Black;
                StatusText.Text = $"Scanned: {ISBNValidator.FormatISBN(barcode)}";
                ScanCountText.Text = _scanCount.ToString();

                // Update tray icon count
                _trayIconManager.UpdateTodayCount(_scanCount);

                // Clear input field
                ScannerInputField.Clear();

                // Show notification if app is not focused
                if (!IsActive && sender != this)
                    _trayIconManager.ShowNotification(
                        "Book Scanned",
                        $"ISBN: {ISBNValidator.FormatISBN(barcode)}"
                    );
            });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (BackgroundScanningCheckbox.IsChecked == true)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                    StatusText.Foreground = Brushes.Black;
                    StatusText.Text = "📚 Scanner active in background";
                    _trayIconManager.ShowNotification(
                        "myAthenaeio",
                        "Scanner is active in background");
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                    StatusText.Foreground = Brushes.Black;
                    StatusText.Text = "Scanner disabled (minimized)";
                }
            }
            else
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                StatusText.Foreground = Brushes.Black;
                StatusText.Text = "Ready to scan";
            }

            UpdateCurrentModeText();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                UpdateCurrentModeText();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                if (BackgroundScanningCheckbox.IsChecked == true)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                }
                UpdateCurrentModeText();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _trayIconManager?.Dispose();
            _scannerManager?.Dispose();
        }

        private void BackgroundScanning_Changed(object sender, RoutedEventArgs e)
        {
            if (_scannerManager == null) return;

            if (BackgroundScanningCheckbox.IsChecked == true)
            {
                // Will trigger permission dialog on first enable
                if (WindowState == WindowState.Minimized || !IsActive)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                }
            }
            else
            {
                if (WindowState == WindowState.Minimized || !IsActive)
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                }
            }

            UpdateCurrentModeText();
        }

        private void UpdateCurrentModeText()
        {
            string modeText = WindowState == WindowState.Minimized
                ? (BackgroundScanningCheckbox.IsChecked == true ? "Background Service (Active)" : "Disabled (Minimized)")
                : (IsActive ? "Focused Field Only" :
                   (BackgroundScanningCheckbox.IsChecked == true ? "Background Service (Active)" : "Disabled"));

            CurrentModeText.Text = modeText;
        }
    }

    public class ScanLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Barcode { get; set; } = String.Empty;
        public string Source { get; set; } = string.Empty;
    }
}