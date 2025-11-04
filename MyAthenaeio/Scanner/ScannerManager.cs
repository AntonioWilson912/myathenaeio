using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyAthenaeio.Scanner
{
    internal class ScannerManager
    {
        private ScannerMode _currentMode = ScannerMode.Disabled;
        private GlobalKeyboardHook _globalHook; // Only if user enables
        private BarcodeScanner _scanner;
        private bool _backgroundModeEnabled = false;

        public event EventHandler<string> BarcodeScanned;
        public bool BackgroundModeEnabled => _backgroundModeEnabled;

        public ScannerManager()
        {
            _scanner = new BarcodeScanner();
            _scanner.BarcodeScanned += (s, barcode) => BarcodeScanned?.Invoke(this, barcode);
            _globalHook = new GlobalKeyboardHook();
        }

        public void SetMode(ScannerMode mode)
        {
            // Clean up previous mode
            if (_currentMode == ScannerMode.BackgroundService && _globalHook != null)
                _globalHook.Unhook();

            _currentMode = mode;

            // Set up new mode
            if (mode == ScannerMode.BackgroundService)
            {
                if (!_backgroundModeEnabled)
                {
                    ShowBackgroundModeDialog();
                    return;
                }
                SetupGlobalHook();
            }
        }

        public void ProcessKey(Key key)
        {
            // For focused field mode
            if (_currentMode == ScannerMode.FocusedFieldOnly)
                _scanner.OnKeyPress(key);
        }

        private void ShowBackgroundModeDialog()
        {
            var result = MessageBox.Show(
                "Background scanning mode requires monitoring keyboard input " +
                "when the app is minimized.\n\n" +
                "This allows you to scan books from your bookshelf while away from your computer.\n\n" +
                "The app ONLY processes:\n" +
                "- ISBN barcode patterns (10-13 digits)\n" +
                "  typed at scanner speeds (100+ chars/second)\n" +
                "- Regular typing and passwords are ignored\n\n" +
                "Enable background scanning?",
                "Background Scanning Permission",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _backgroundModeEnabled = true;
                SetupGlobalHook();
            }
            else
            {
                _currentMode = ScannerMode.Disabled;
            }
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
