using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Navigation;

namespace MyAthenaeio.Utils
{
    internal static class ISBNValidator
    {
        private const int MIN_ISBN_LENGTH = 10; // Minimum valid ISBN
        private const int MAX_ISBN_LENGTH = 13; // Maximum valid ISBN

        /// <summary>
        /// Cleans ISBN by removing dashes, spaces, and other formatting.
        /// </summary>
        public static string CleanISBN(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return string.Empty;

            return isbn.Replace("-", "").Replace(" ", "").ToUpper().Trim();
        }

        /// <summary>
        /// Validates if string is a valid ISBN format (10 or 13 digits)
        /// </summary>
        public static bool IsValidISBNFormat(string barcode)
        {
            // Remove prefixes/formatting
            string cleaned = CleanISBN(barcode);

            // ISBN13 must be exactly 13 digits
            if (cleaned.Length == MAX_ISBN_LENGTH)
                return cleaned.All(char.IsDigit);

            // ISBN-10 must be 10 characters where first 9 are digits and last can be digit or X
            if (cleaned.Length == MIN_ISBN_LENGTH)
            {
                // First 9 must be digits
                if (!cleaned[..9].All(char.IsDigit))
                    return false;

                // Last character can be digit or X
                char lastChar = cleaned[9];
                return char.IsDigit(lastChar) || lastChar == 'X';
            }

            return false;
        }

        /// <summary>
        /// Validates ISBN-10 checksum
        /// The check digit can be 0-9 or X (representing 10)
        /// </summary>
        public static bool ValidateISBN10(string isbn)
        {
            string cleaned = CleanISBN(isbn);

            if (cleaned.Length != MIN_ISBN_LENGTH)
                return false;

            if (!cleaned[..9].All(char.IsDigit))
                return false;


            int sum = 0;
            for (int i = 0; i < 9; i++)
                sum += (cleaned[i] - '0') * (10 - i);

            char lastChar = cleaned[9];
            int checkDigit;

            if (lastChar == 'X')
                checkDigit = 10;
            else if (char.IsDigit(lastChar))
                checkDigit = lastChar - '0';
            else
                return false;

            sum += checkDigit;

            return sum % 11 == 0;
        }

        /// <summary>
        /// Validates ISBN-13 checksum
        /// </summary>
        public static bool ValidateISBN13(string isbn)
        {
            string cleaned = CleanISBN(isbn);

            if (cleaned.Length != MAX_ISBN_LENGTH || !cleaned.All(char.IsDigit))
                return false;

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = cleaned[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int checkDigit = cleaned[12] - '0';
            int calculatedCheck = (10 - (sum % 10)) % 10;

            return checkDigit == calculatedCheck;
        }

        /// <summary>
        /// Validates ISBN format and optionally validates checksum
        /// </summary>
        public static bool IsValidISBN(string isbn, bool validateChecksum = false)
        {
            if (!IsValidISBNFormat(isbn))
                return false;

            if (!validateChecksum)
                return true;

            string cleaned = CleanISBN(isbn);

            return cleaned.Length switch
            {
                MIN_ISBN_LENGTH => ValidateISBN10(cleaned),
                MAX_ISBN_LENGTH => ValidateISBN13(cleaned),
                _ => false
            };
        }

        /// <summary>
        /// Converts ISBN-10 to ISBN-13 format
        /// </summary>
        public static string ConvertISBN10To13(string isbn10)
        {
            string cleaned = CleanISBN(isbn10);

            if (cleaned.Length != MIN_ISBN_LENGTH)
                return isbn10;

            // Remove check digit and add 978 digits
            string isbn13Base = string.Concat("978", cleaned.AsSpan(0, 9));

            // Calculate new check digit
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = cleaned[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            

            return isbn13Base + checkDigit;
        }

        /// <summary>
        /// Formats ISBN with dashes for display
        /// </summary>
        public static string FormatISBN(string isbn)
        {
            string cleaned = CleanISBN(isbn);

            if (cleaned.Length == MIN_ISBN_LENGTH)
            {
                // Format: X-XXX-XXXXX-X
                return $"{cleaned.Substring(0, 1)}-{cleaned.Substring(1,3)}-{cleaned.Substring(4, 5)}-{cleaned.Substring(9, 1)}";
            }
            if (cleaned.Length == MAX_ISBN_LENGTH)
            {
                // Format: XXX-X-XXX-XXXXX-X
                return $"{cleaned.Substring(0, 3)}-{cleaned.Substring(3, 1)}-{cleaned.Substring(4, 3)}-{cleaned.Substring(7, 5)}-{cleaned.Substring(12, 1)}";
            }

            return isbn;
        }
    }
}
