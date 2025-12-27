namespace MyAthenaeio.Models.ViewModels
{
    public class BorrowerIncludeOptions
    {
        public bool IncludeLoans { get; set; }
        public bool ForceReload { get; set; }

        public static BorrowerIncludeOptions Default => new()
        {
            IncludeLoans = false  // Usually don't need loans when listing borrowers
        };

        public static BorrowerIncludeOptions None => new();

        public static BorrowerIncludeOptions WithLoans => new()
        {
            IncludeLoans = true
        };
    }
}