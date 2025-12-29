using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using MyAthenaeio.Scanner;

namespace MyAthenaeio.Services
{
    internal class TrayIconManager : IDisposable
    {
        private TaskbarIcon? _taskbarIcon;
        private readonly ScannerManager _scannerManager;
        private int _todayCount = 0;

        public TrayIconManager(ScannerManager scannerManager)
        {
            _scannerManager = scannerManager;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _taskbarIcon = new TaskbarIcon
            {
                ToolTipText = "myAthenaeio - Book Scanner",
                ContextMenu = CreateContextMenu()
            };

            // Load custom icon
            var iconPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Icons", "tray.ico");

            if (System.IO.File.Exists(iconPath))
            {
                _taskbarIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(iconPath, UriKind.Absolute));
            }
            // Note: H.NotifyIcon will use a default icon if none is set

            // Double-click to open main window
            _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();

            // Subscribe to scanner events
            _scannerManager.BarcodeScanned += OnBookScanned;
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
            openItem.Click += (s, e) => ShowMainWindow();
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
            pauseItem.Click += (s, e) => ToggleScanner();
            menu.Items.Add(pauseItem);

            menu.Items.Add(new Separator());

            // Exit
            var exitItem = new MenuItem
            {
                Header = "❌ Exit"
            };
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            // Update menu dynamically when opened
            menu.Opened += (s, e) =>
            {
                statusItem.Header = $"Scanned today: {_todayCount}";
                modeItem.Header = _scannerManager.BackgroundModeEnabled
                    ? "Scanner: Active (Background)"
                    : "Scanner: Active (Focused)";
            };

            return menu;
        }

        private void OnBookScanned(object? sender, string barcode)
        {
            _todayCount++;

            // Show balloon notification
            ShowNotification("Book Scanned", $"ISBN: {barcode}\nTotal today: {_todayCount}");

            // Update tray tooltip
            if (_taskbarIcon != null)
                _taskbarIcon.ToolTipText = $"myAthenaeio - {_todayCount} scanned today";
        }

        public void ShowNotification(string title, string message)
        {
            _taskbarIcon?.ShowNotification(title, message);
        }

        private static void ShowMainWindow()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        }

        private void ToggleScanner()
        {
            // TODO: Implement pause/resume functionality
            ShowNotification("Scanner", "Pause/Resume coming soon!");
        }

        private static void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        public void UpdateTodayCount(int count)
        {
            _todayCount = count;

            if (_taskbarIcon != null)
                _taskbarIcon.ToolTipText = $"myAthenaeio - {_todayCount} scanned today";
        }

        public void Dispose()
        {
            _taskbarIcon?.Dispose();
        }
    }
}