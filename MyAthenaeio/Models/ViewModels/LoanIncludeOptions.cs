namespace MyAthenaeio.Models.ViewModels
{
    public class LoanIncludeOptions
    {
        public bool IncludeBook { get; set; }
        public bool IncludeBookAuthors { get; set; }
        public bool IncludeBorrower { get; set; }
        public bool IncludeRenewals { get; set; }
        public bool ForceReload { get; set; }

        public static LoanIncludeOptions Default => new()
        {
            IncludeBook = true,
            IncludeBookAuthors = true,
            IncludeBorrower = true,
            IncludeRenewals = true
        };

        public static LoanIncludeOptions None => new();

        public static LoanIncludeOptions Minimal => new()
        {
            IncludeBook = true,
            IncludeBorrower = true
        };
    }
}