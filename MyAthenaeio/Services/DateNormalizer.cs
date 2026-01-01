using System.Text.RegularExpressions;

namespace MyAthenaeio.Services
{
    public static class DateNormalizer
    {
        public static string? NormalizeDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            // Already in YYYY-MM-DD format
            if (Regex.IsMatch(dateString, @"^\d{4}-\d{2}-\d{2}$"))
                return dateString;

            // Try parsing with various formats
            string[] formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "MM/dd/yyyy",
                "dd/MM/yyyy",
                "MMMM d, yyyy",
                "MMMM dd, yyyy",
                "d MMMM yyyy",
                "dd MMMM yyyy",
                "MMM d, yyyy",
                "MMM dd, yyyy",
                "d MMM yyyy",
                "dd MMM yyyy",
                "yyyy",
                "MMMM yyyy",
                "MMM yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime result))
                {
                    return result.ToString("yyyy-MM-dd");
                }
            }

            // Try just extracting year
            var yearMatch = Regex.Match(dateString, @"\d{4}");
            if (yearMatch.Success)
            {
                return $"{yearMatch.Value}-01-01";
            }

            return null;
        }
    }
}