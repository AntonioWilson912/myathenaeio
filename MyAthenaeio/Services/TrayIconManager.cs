using System.Windows;
using MyAthenaeio.Scanner;
using Application = System.Windows.Application;

namespace MyAthenaeio.Services
{
    internal class TrayIconManager : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private ScannerManager _scannerManager;
        private int _todayCount = 0;

        public TrayIconManager(ScannerManager scannerManager)
        {
            _scannerManager = scannerManager;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            // Load custom icon
            var iconPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Icons", "tray.ico");

            Icon icon;
            if (System.IO.File.Exists(iconPath))
            {
                icon = new Icon(iconPath);
            }
            else
            {
                // Fallback to system icon
                icon = SystemIcons.Application;
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Visible = true,
                Text = "myAthenaeio - Book Scanner"
            };

            _notifyIcon.DoubleClick += TrayIcon_DoubleClick;
            _notifyIcon.ContextMenuStrip = CreateContextMenu();

            // Subscribe to scanner events
            _scannerManager.BarcodeScanned += OnBookScanned;
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            // Open main window
            var openItem = new ToolStripMenuItem("📚 Open Library", null, (s, e) => ShowMainWindow());
            openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(openItem);

            menu.Items.Add(new ToolStripSeparator());

            // Status item (non-clicktable)
            var statusItem = new ToolStripMenuItem($"Scanned today: {_todayCount}")
            {
                Enabled = false
            };
            menu.Items.Add(statusItem);

            // Scanner mode status
            var modeItem = new ToolStripMenuItem("Scanner: Active")
            {
                Enabled = false
            };
            menu.Items.Add(modeItem);

            menu.Items.Add(new ToolStripSeparator());

            // Pause/Resume scanner
            var pauseItem = new ToolStripMenuItem("⏸️ Pause Scanner", null, (s, e) => ToggleScanner());
            menu.Items.Add(pauseItem);

            menu.Items.Add(new ToolStripSeparator());

            // Exit
            menu.Items.Add("❌ Exit", null, (s, e) => ExitApplication());

            // Update menu dynamically when opened
            menu.Opening += (s, e) =>
            {
                statusItem.Text = $"Scanned today: {_todayCount}";
                modeItem.Text = _scannerManager.BackgroundModeEnabled
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
            if (_notifyIcon != null)
                _notifyIcon.Text = $"myAthenaeio - {_todayCount} scanned today";
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, icon);
            }
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
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
            ShowNotification("Scanner", "Pause/Resume coming soon!", ToolTipIcon.Warning);
        }

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        public void UpdateTodayCount(int count)
        {
            _todayCount = count;

            if (_notifyIcon != null)
                _notifyIcon.Text = $"myAthenaeio - {_todayCount} scanned today";
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }
}
