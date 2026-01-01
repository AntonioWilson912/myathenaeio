using H.NotifyIcon;
using MyAthenaeio.Scanner;
using MyAthenaeio.Views;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;

namespace MyAthenaeio.Services
{
    internal class TrayIconManager : IDisposable
    {
        private TaskbarIcon? _taskbarIcon;
        private readonly ScannerManager _scannerManager;
        private int _todayCount = 0;
        private bool _isInitialized = false;
        private bool _scannerPaused = false;

        public bool IsPaused => _scannerPaused;

        public TrayIconManager(ScannerManager scannerManager)
        {
            _scannerManager = scannerManager;
            Application.Current.Dispatcher.Invoke(() =>
            {
                InitializeTrayIcon();
            });
        }

        private void InitializeTrayIcon()
        {
            try
            {
                _taskbarIcon = new TaskbarIcon
                {
                    ToolTipText = "myAthenaeio - Book Scanner",
                    ContextMenu = CreateContextMenu()
                };

                try
                {
                    var iconUri = new Uri("pack://application:,,,/Resources/Icons/tray.ico");
                    _taskbarIcon.IconSource = new BitmapImage(iconUri);
                    Debug.WriteLine("Icon loaded successfully from embedded resource.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load embedded icon: {ex.Message}");
                    _taskbarIcon.IconSource = CreateFallbackIcon();
                }

                _taskbarIcon.ForceCreate();

                // Double-click to restore main window properly
                _taskbarIcon.TrayMouseDoubleClick += (s, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ShowMainWindow();
                    });
                };

                // Subscribe to scanner events
                _scannerManager.BarcodeScanned += OnBookScanned;
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize tray icon: {ex.Message}");
                _isInitialized = false;
            }
        }

        private static BitmapImage? CreateFallbackIcon()
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "myAthenaeio_tray_fallback.ico");

                // Only create if doesn't exist
                if (!File.Exists(tempPath))
                {
                    // Create bitmap using System.Drawing
                    using var bitmap = new Bitmap(16, 16);
                    using var graphics = Graphics.FromImage(bitmap);

                    // Background
                    graphics.Clear(Color.DarkBlue);

                    // Simple book shape
                    graphics.FillRectangle(
                        Brushes.White,
                        3, 3, 10, 10);

                    // Book spine
                    using var pen = new Pen(Color.DarkBlue, 1);
                    graphics.DrawLine(pen, 8, 3, 8, 13);

                    // Save as .ico
                    using var icon = Icon.FromHandle(bitmap.GetHicon());
                    using var fileStream = new FileStream(tempPath, FileMode.Create);
                    icon.Save(fileStream);
                }

                // Load as BitmapImage
                var bitmapImage = new BitmapImage(new Uri(tempPath, UriKind.Absolute));
                bitmapImage.Freeze();
                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create fallback icon: {ex.Message}");
                return null;
            }
        }

        private ContextMenu CreateContextMenu()
        {
            var menu = new ContextMenu();

            // Open main window
            var openItem = new MenuItem
            {
                Header = "📚 Open Library",
                FontWeight = FontWeights.Bold
            };
            openItem.Click += (s, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowMainWindow();
                });
            };
            menu.Items.Add(openItem);

            menu.Items.Add(new Separator());

            // Status item (non-clickable)
            var statusItem = new MenuItem
            {
                Header = $"Scanned today: {_todayCount}",
                IsEnabled = false
            };
            menu.Items.Add(statusItem);

            // Scanner mode status
            var modeItem = new MenuItem
            {
                Header = "Scanner: Active",
                IsEnabled = false
            };
            menu.Items.Add(modeItem);

            menu.Items.Add(new Separator());

            // Pause/Resume scanner
            var pauseItem = new MenuItem
            {
                Header = "⏸️ Pause Scanner"
            };
            pauseItem.Click += (s, e) => ToggleScanner(pauseItem);
            menu.Items.Add(pauseItem);

            menu.Items.Add(new Separator());

            // Exit
            var exitItem = new MenuItem
            {
                Header = "❌ Exit"
            };
            exitItem.Click += (s, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ExitApplication();
                });
            };
            menu.Items.Add(exitItem);

            // Update menu dynamically when opened
            menu.Opened += (s, e) =>
            {
                statusItem.Header = $"Scanned today: {_todayCount}";

                if (_scannerPaused)
                {
                    pauseItem.Header = "▶️ Resume Scanner";
                    modeItem.Header = "Scanner: Paused";
                }
                else
                {
                    pauseItem.Header = "⏸️ Pause Scanner";
                    modeItem.Header = _scannerManager.BackgroundModeEnabled
                        ? "Scanner: Active (Background)"
                        : "Scanner: Active (Focused)";
                }
            };

            return menu;
        }

        private void OnBookScanned(object? sender, string barcode)
        {
            _todayCount++;

            // Show balloon notification
            ShowNotification("Book Scanned", $"ISBN: {barcode}\nTotal today: {_todayCount}");

            // Update tray tooltip
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_taskbarIcon != null && _isInitialized)
                    _taskbarIcon.ToolTipText = $"myAthenaeio - {_todayCount} scanned today";
            });
        }

        public void ShowNotification(string title, string message)
        {
            // Check if initialized before showing notification
            if (!_isInitialized || _taskbarIcon == null)
            {
                Debug.WriteLine($"Tray icon not ready. Notification skipped: {title} - {message}");
                return;
            }

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon?.ShowNotification(title, message);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        private static void ShowMainWindow()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                // Handle both minimized and hidden states
                mainWindow.Show();
                mainWindow.ShowInTaskbar = true;

                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }

                mainWindow.Activate();
                mainWindow.Focusable = true;
                mainWindow.Focus();

                // Bring to front
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
            }
        }

        private void ToggleScanner(MenuItem menuItem)
        {
            _scannerPaused = !_scannerPaused;

            if (_scannerPaused)
            {
                // Pause scanner
                _scannerManager.SetMode(ScannerMode.Disabled);
                menuItem.Header = "▶️ Resume Scanner";
                ShowNotification("Scanner Paused", "Barcode scanning is paused");
            }
            else
            {
                // Resume scanner
                var mainWindow = Application.Current.MainWindow;

                if (mainWindow != null && mainWindow.WindowState == WindowState.Minimized &&
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        return mainWindow is MainWindow mw &&
                               mw._settingsService.Settings.BackgroundScanningEnabled;
                    }))
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                }

                menuItem.Header = "⏸️ Pause Scanner";
                ShowNotification("Scanner Resumed", "Barcode scanning is active");
            }
        }

        private static void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        public void UpdateTodayCount(int count)
        {
            if (!_isInitialized || _taskbarIcon == null)
                return;

            _todayCount = count;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_taskbarIcon != null)
                    _taskbarIcon.ToolTipText = $"myAthenaeio - {_todayCount} scanned today";
            });
        }

        public void Dispose()
        {
            if (_isInitialized && _taskbarIcon != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon?.Dispose();
                });
            }
        }
    }
}