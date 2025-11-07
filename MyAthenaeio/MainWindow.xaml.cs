using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MyAthenaeio.Models;
using MyAthenaeio.Scanner;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;

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
            Dispatcher.Invoke(async () =>
            {
                _scanCount++;

                // Fetch ISBN details
                Result<Book> bookResult = await BookAPIService.FetchBookByISBN(barcode);
                if (!bookResult.IsSuccess)
                {
                    //Only show error if window is active or this is a manual scan
                    if (IsActive || sender == this)
                    {
                        MessageBox.Show(bookResult.Error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        // Show tray notification for background errors
                        _trayIconManager.ShowNotification(
                            "Scan Failed",
                            $"Failed to fetch book: {barcode}");
                    }
                    return;
                }

                Book book = bookResult.Value!;

                // Add to log
                _scanLog.Insert(0, new ScanLogEntry
                {
                    Timestamp = DateTime.Now,
                    Barcode = ISBNValidator.FormatISBN(barcode),
                    Title = book.Title,
                    Cover = book.Cover!,
                    Source = sender == this ? "Manual" : (IsActive ? "Scanner" : "Background")
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
                // User wants background scanning
                if (BackgroundScanningCheckbox.IsChecked == true)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);

                    // Check if user actually approved it (might have said no to dialog)
                    if (_scannerManager.BackgroundModeEnabled)
                    {
                        StatusText.Foreground = Brushes.Black;
                        StatusText.Text = "📚 Scanner active in background";
                        _trayIconManager.ShowNotification(
                            "myAthenaeio",
                            "Scanner is active in background");
                    }
                    else
                    {
                        // User declined the permission - uncheck the box
                        BackgroundScanningCheckbox.IsChecked = false;
                        StatusText.Foreground = Brushes.Black;
                        StatusText.Text = "Scanner disabled (minimized)";
                    }
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
                // Window is normal or maximized - use focused mode
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
            // Only handle background mode if minimized
            // Don't trigger when just clicking away from the window
            if (WindowState == WindowState.Minimized)
            {
                if (BackgroundScanningCheckbox.IsChecked == true && _scannerManager.BackgroundModeEnabled)
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

            // Only do something if the window is currently minimized or inactive
            if (WindowState == WindowState.Minimized)
            {
                if (BackgroundScanningCheckbox.IsChecked == true)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);

                    // If user declined permission, uncheck
                    if (!_scannerManager.BackgroundModeEnabled)
                    {
                        BackgroundScanningCheckbox.IsChecked = false;
                    }
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                }

                UpdateCurrentModeText();
            }
            // If window is visible, just update the text - mode will change on minimize
        }

        private void UpdateCurrentModeText()
        {
            string modeText = WindowState == WindowState.Minimized
                ? (BackgroundScanningCheckbox.IsChecked == true && _scannerManager.BackgroundModeEnabled
                    ? "Background Service (Active)"
                    : "Disabled (Minimized)")
                : (IsActive ? "Focused Field Only" : "Disabled");

            CurrentModeText.Text = modeText;
        }
    }

    public class ScanLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public BitmapImage Cover { get; set; } = default!;
        public string Source { get; set; } = string.Empty;
    }
}