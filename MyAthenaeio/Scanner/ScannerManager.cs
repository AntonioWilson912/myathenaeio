using System.Windows;
using System.Windows.Input;
using MyAthenaeio.Services;

namespace MyAthenaeio.Scanner
{
    public class ScannerManager : IDisposable
    {
        private ScannerMode _currentMode = ScannerMode.Disabled;
        private GlobalKeyboardHook? _globalHook;
        private BarcodeScanner _scanner;
        private bool _backgroundModeEnabled = false;
        private bool _isShowingDialog = false;
        private readonly SettingsService _settingsService;

        public event EventHandler<string>? BarcodeScanned;
        public bool BackgroundModeEnabled => _backgroundModeEnabled;

        public ScannerManager(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _scanner = new BarcodeScanner();
            _scanner.BarcodeScanned += (s, barcode) => BarcodeScanned?.Invoke(this, barcode);

            // Initialize background mode from settings
            _backgroundModeEnabled = _settingsService.Settings.BackgroundScanningEnabled;
        }

        public void SetMode(ScannerMode mode)
        {
            if (_isShowingDialog)
                return;

            // Clean up previous mode
            if (_currentMode == ScannerMode.BackgroundService && _globalHook != null)
                _globalHook.Unhook();

            // Set up new mode
            if (mode == ScannerMode.BackgroundService)
            {
                if (!_backgroundModeEnabled)
                {
                    ShowBackgroundModeDialog();
                    return;
                }
                SetupGlobalHook();
                _currentMode = mode;
            }
            else
            {
                _currentMode = mode;
            }
        }

        public void ProcessKey(Key key)
        {
            if (_currentMode == ScannerMode.FocusedFieldOnly)
                _scanner.OnKeyPress(key);
        }

        private void ShowBackgroundModeDialog()
        {
            if (_isShowingDialog)
                return;

            _isShowingDialog = true;
            var result = MessageBox.Show(
                "Background scanning mode requires monitoring keyboard input " +
                "when the app is minimized.\n\n" +
                "This allows you to scan books from your bookshelf while away from your computer.\n\n" +
                "The app ONLY processes:\n" +
                "• ISBN barcode patterns (10-13 digits)\n" +
                "  typed at scanner speeds (100+ chars/second)\n" +
                "• Regular typing and passwords are ignored\n\n" +
                "Enable background scanning?",
                "Background Scanning Permission",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _backgroundModeEnabled = true;
                _settingsService.Settings.BackgroundScanningEnabled = true;
                _settingsService.SaveSettings();
                SetupGlobalHook();
                _currentMode = ScannerMode.BackgroundService;
            }
            else
            {
                _backgroundModeEnabled = false;
                _currentMode = ScannerMode.Disabled;
            }

            _isShowingDialog = false;
        }

        private void SetupGlobalHook()
        {
            if (_globalHook == null)
            {
                _globalHook = new GlobalKeyboardHook();
                _globalHook.KeyPressed += (s, key) =>
                {
                    _scanner.OnKeyPress(key);
                };
            }
            _globalHook.Hook();
        }

        public void Dispose()
        {
            _globalHook?.Dispose();
        }
    }
}