using System.Text.RegularExpressions;

namespace MyAthenaeio.Utils
{
    public static class EmailValidationService
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        /// <summary>
        /// Validates email format. Returns null if valid, error message if invalid.
        /// </summary>
        public static string? GetValidationError(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Email cannot be empty.";

            email = email.Trim();

            if (email.Length > 200)
                return "Email cannot exceed 200 characters.";

            if (!EmailRegex.IsMatch(email))
                return "Email format is invalid. Please enter a valid email address (e.g., user@example.com).";

            // Additional checks
            if (email.StartsWith(".") || email.EndsWith("."))
                return "Email cannot start or end with a period.";

            if (email.Contains(".."))
                return "Email cannot contain consecutive periods.";

            var parts = email.Split('@');
            if (parts.Length != 2)
                return "Email must contain exactly one @ symbol.";

            if (parts[0].Length == 0)
                return "Email must have a username before the @ symbol.";

            if (parts[1].Length == 0 || !parts[1].Contains('.'))
                return "Email must have a valid domain after the @ symbol.";

            return null; // Valid
        }

        /// <summary>
        /// Checks if email format is valid.
        /// </summary>
        public static bool IsValid(string email)
        {
            return GetValidationError(email) == null;
        }
    }
}