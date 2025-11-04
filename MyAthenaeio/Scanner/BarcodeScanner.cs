using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyAthenaeio.Scanner
{
    internal class BarcodeScanner
    {
        private StringBuilder _barcodeBuffer = new StringBuilder();
        private DateTime _lastKeyPress = DateTime.Now;
        private DateTime _scanStartTime = DateTime.Now;

        private const int BARCODE_TIME_OUT_MS = 100; // Time between keypresses
        private const int MAX_SCAN_DURATION_MS = 300; // Total scan duration
        private const int MIN_BARCODE_LENGTH = 8; // Minimum valid barcode
        private const int MAX_BARCODE_LENGTH = 18; // Maximum valid barcode
        private const int MIN_ISBN_LENGTH = 10; // Minimum valid ISBN
        private const int MAX_ISBN_LENGTH = 13; // Maximum valid ISBN
        private const int MIN_KEYS_PER_SECOND = 50; // Scanner speed

        public event EventHandler<string> BarcodeScanned;

        public void OnKeyPress(Key key)
        {
            var now = DateTime.Now;

            // Reset buffer if too much time has passed
            if ((now - _lastKeyPress).TotalMilliseconds > BARCODE_TIME_OUT_MS)
            {
                _barcodeBuffer.Clear();
                _scanStartTime = now;
            }

            _lastKeyPress = DateTime.Now;

            if (key == Key.Return || key == Key.Enter)
            {
                // Scanner captured barcode
                string barcode = _barcodeBuffer.ToString();
                if (IsValidBarcodeInput(barcode, now))
                {
                    BarcodeScanned?.Invoke(this, barcode);
                }
                _barcodeBuffer.Clear();
            }
            else
            {
                // Build up the barcode string
                string keyChar = GetCharFromKey(key);
                if (!string.IsNullOrEmpty(keyChar))
                {
                    _barcodeBuffer.Append(keyChar);
                }
            }
        }

        private bool IsValidBarcodeInput(string barcode, DateTime endTime)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            // Length validation
            int length = barcode.Length;
            if (length < MIN_BARCODE_LENGTH || length > MAX_BARCODE_LENGTH)
                return false;

            // Duration validation
            var duration = (endTime - _scanStartTime).TotalMilliseconds;
            if (duration > MAX_SCAN_DURATION_MS)
                return false;

            // Speed validation
            double keysPerSecond = length / duration * 1000;
            if (keysPerSecond < MIN_KEYS_PER_SECOND)
                return false;

            // Content validation
            if (!IsValidISBNFormat(barcode))
                return false;

            return true;
        }

        private static bool IsValidISBNFormat(string barcode)
        {
            // Remove prefixes/formatting
            string cleaned = barcode.Replace("-", "").Replace(" ", "");

            // ISBN-10 or ISBN-13 should be all digits
            if (!cleaned.All(char.IsDigit))
                return false;

            // Valid ISBN lengths
            return cleaned.Length == MIN_ISBN_LENGTH || cleaned.Length == MAX_ISBN_LENGTH;
        }

        private static string GetCharFromKey(Key key)
        {
            // Handle numbers
            if (key >= Key.D0 && key <= Key.D9)
                return ((char)('0' + (key - Key.D0))).ToString();
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return ((char)('0' + (key - Key.NumPad0))).ToString();

            if (key >= Key.A && key <= Key.Z)
                return key.ToString();
            return string.Empty;
        }
    }
}
