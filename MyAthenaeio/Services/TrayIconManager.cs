using H.NotifyIcon;
using MyAthenaeio.Scanner;
using MyAthenaeio.Views;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using Serilog;

namespace MyAthenaeio.Services
{
    internal class TrayIconManager : IDisposable
    {
        private static readonly ILogger _logger = Log.ForContext<TrayIconManager>();
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
                _logger.Debug("Initializing tray icon");

                _taskbarIcon = new TaskbarIcon
                {
                    ToolTipText = "myAthenaeio - Book Scanner",
                    ContextMenu = CreateContextMenu()
                };

                try
                {
                    var iconUri = new Uri("pack://application:,,,/Resources/Icons/tray.ico");
                    _taskbarIcon.IconSource = new BitmapImage(iconUri);
                    _logger.Debug("Tray icon loaded from embedded resource");
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to load embedded tray icon, using fallback");
                    _taskbarIcon.IconSource = CreateFallbackIcon();
                }

                _taskbarIcon.ForceCreate();

                _taskbarIcon.TrayMouseDoubleClick += (s, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ShowMainWindow();
                    });
                };

                _scannerManager.BarcodeScanned += OnBookScanned;
                _isInitialized = true;

                _logger.Information("Tray icon initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize tray icon");
                _isInitialized = false;
            }
        }

        private static BitmapImage? CreateFallbackIcon()
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "myAthenaeio_tray_fallback.ico");

                if (!File.Exists(tempPath))
                {
                    using var bitmap = new Bitmap(16, 16);
                    using var graphics = Graphics.FromImage(bitmap);

                    graphics.Clear(Color.DarkBlue);
                    graphics.FillRectangle(Brushes.White, 3, 3, 10, 10);

                    using var pen = new Pen(Color.DarkBlue, 1);
                    graphics.DrawLine(pen, 8, 3, 8, 13);

                    using var icon = Icon.FromHandle(bitmap.GetHicon());
                    using var fileStream = new FileStream(tempPath, FileMode.Create);
                    icon.Save(fileStream);
                }

                var bitmapImage = new BitmapImage(new Uri(tempPath, UriKind.Absolute));
                bitmapImage.Freeze();
                return bitmapImage;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create fallback icon");
                return null;
            }
        }

        private ContextMenu CreateContextMenu()
        {
            var menu = new ContextMenu();

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

            var statusItem = new MenuItem
            {
                Header = $"Scanned today: {_todayCount}",
                IsEnabled = false
            };
            menu.Items.Add(statusItem);

            var modeItem = new MenuItem
            {
                Header = "Scanner: Active",
                IsEnabled = false
            };
            menu.Items.Add(modeItem);

            menu.Items.Add(new Separator());

            var pauseItem = new MenuItem
            {
                Header = "⏸️ Pause Scanner"
            };
            pauseItem.Click += (s, e) => ToggleScanner(pauseItem);
            menu.Items.Add(pauseItem);

            menu.Items.Add(new Separator());

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

            _logger.Information("Book scanned via tray: {Barcode} (Today's count: {Count})",
                barcode, _todayCount);

            ShowNotification("Book Scanned", $"ISBN: {barcode}\nTotal today: {_todayCount}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_taskbarIcon != null && _isInitialized)
                    _taskbarIcon.ToolTipText = $"myAthenaeio - {_todayCount} scanned today";
            });
        }

        public void ShowNotification(string title, string message)
        {
            if (!_isInitialized || _taskbarIcon == null)
            {
                _logger.Debug("Tray icon not ready, notification skipped: {Title}", title);
                return;
            }

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon?.ShowNotification(title, message);
                });

                _logger.Debug("Tray notification shown: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to show tray notification: {Title}", title);
            }
        }

        private static void ShowMainWindow()
        {
            _logger.Debug("Showing main window from tray");

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.ShowInTaskbar = true;

                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }

                mainWindow.Activate();
                mainWindow.Focusable = true;
                mainWindow.Focus();

                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
            }
        }

        private void ToggleScanner(MenuItem menuItem)
        {
            _scannerPaused = !_scannerPaused;

            if (_scannerPaused)
            {
                _scannerManager.SetMode(ScannerMode.Disabled);
                menuItem.Header = "▶️ Resume Scanner";
                ShowNotification("Scanner Paused", "Barcode scanning is paused");
                _logger.Information("Scanner paused via tray");
            }
            else
            {
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
                _logger.Information("Scanner resumed via tray");
            }
        }

        private static void ExitApplication()
        {
            _logger.Information("Application exit requested from tray");
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
                _logger.Debug("Disposing tray icon");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon?.Dispose();
                });
            }
        }
    }
}