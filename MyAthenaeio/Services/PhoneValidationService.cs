using System.Text.RegularExpressions;

namespace MyAthenaeio.Utils
{
    public static class PhoneValidationService
    {
        // Matches various formats: (123) 456-7890, 123-456-7890, 1234567890, +1 123 456 7890, etc.
        private static readonly Regex PhoneRegex = new(
            @"^[\d\s\-\(\)\+\.]+$",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Validates phone number format. Returns null if valid, error message if invalid.
        /// </summary>
        public static string? GetValidationError(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "Phone number cannot be empty.";

            phone = phone.Trim();

            if (phone.Length > 50)
                return "Phone number cannot exceed 50 characters.";

            if (!PhoneRegex.IsMatch(phone))
                return "Phone number can only contain digits, spaces, hyphens, parentheses, plus sign, and periods.";

            // Extract just the digits
            var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length < 10)
                return "Phone number must have at least 10 digits.";

            if (digitsOnly.Length > 15)
                return "Phone number cannot have more than 15 digits.";

            return null; // Valid
        }

        /// <summary>
        /// Checks if phone number format is valid.
        /// </summary>
        public static bool IsValid(string phone)
        {
            return GetValidationError(phone) == null;
        }

        /// <summary>
        /// Formats a phone number to a standard format.
        /// Supports US/Canada and international formats.
        /// </summary>
        public static string? FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Extract only digits
            var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 0)
                return null;

            // US/Canada format: (XXX) XXX-XXXX
            if (digitsOnly.Length == 10)
            {
                return $"({digitsOnly[..3]}) {digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6, 4)}";
            }

            // US/Canada with country code: +1 (XXX) XXX-XXXX
            if (digitsOnly.Length == 11 && digitsOnly[0] == '1')
            {
                return $"+1 ({digitsOnly.Substring(1, 3)}) {digitsOnly.Substring(4, 3)}-{digitsOnly.Substring(7, 4)}";
            }

            // International format: keep as-is but add + if missing
            if (!phone.StartsWith("+") && digitsOnly.Length > 10)
            {
                return "+" + digitsOnly;
            }

            // Return original if already has + or doesn't match patterns
            return phone;
        }

        /// <summary>
        /// Cleans and normalizes a phone number (removes formatting).
        /// Returns digits only with optional + prefix.
        /// </summary>
        public static string? NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var hasPlus = phone.TrimStart().StartsWith("+");
            var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 0)
                return null;

            return hasPlus ? "+" + digitsOnly : digitsOnly;
        }
    }
}