using MyAthenaeio.Scanner;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyAthenaeio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScannerManager _scannerManager;
        private ObservableCollection<ScanLogEntry> _scanLog;
        private int _scanCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            _scanLog = new ObservableCollection<ScanLogEntry>();
            ScanLogList.ItemsSource = _scanLog;

            _scannerManager = new ScannerManager();
            _scannerManager.BarcodeScanned += OnBarcodeScanned;

            // Set initial mode when window loads
            Loaded += (s, e) =>
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                UpdateCurrentModeText();
            };
        }

        private void ScannerInputField_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Extract the key and pass to scanner manager
            _scannerManager.ProcessKey(e.Key);
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
                    Barcode = barcode,
                    Source = IsActive ? "Focused" : "Background"
                });

                // Update UI
                StatusText.Text = $"Scanned: {barcode}";
                ScanCountText.Text = _scanCount.ToString();

                // Clear input field
                ScannerInputField.Clear();

                // Show notification if app is not focused
                if (!IsActive)
                    ShowBalloonNotification("Book Scanned", $"ISBN: {barcode}");
            });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (BackgroundScanningCheckbox.IsChecked == true)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                    StatusText.Text = "📚 Scanner active in background";
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                    StatusText.Text = "Scanner disabled (minimized)";
                }
            }
            else
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
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

        private void ShowBalloonNotification(string title, string message)
        {
            // TODO: Implement system tray notifications later
            // For now, just update status bar
            StatusText.Text = $"{title}: {message}";
        }
    }

    public class ScanLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Barcode { get; set; } = String.Empty;
        public string Source { get; set; } = string.Empty;
    }
}