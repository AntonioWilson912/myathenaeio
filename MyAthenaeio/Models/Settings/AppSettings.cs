namespace MyAthenaeio.Models.Settings
{
    public class AppSettings
    {
        // Scanner Settings
        public bool BackgroundScanningEnabled { get; set; } = false;
        public int MaxKeystrokeDelayMs { get; set; } = 100;

        // API Settings
        public int ApiTimeoutSeconds { get; set; } = 30;

        // Loan Settings
        public int DefaultLoanDays { get; set; } = 14;
        public int MaxRenewals { get; set; } = 2;
        public int RenewalPeriodDays { get; set; } = 7;

        // UI Preferences (for future use)
        public string Theme { get; set; } = "Light";
        public int DefaultPageSize { get; set; } = 20;
    }
}