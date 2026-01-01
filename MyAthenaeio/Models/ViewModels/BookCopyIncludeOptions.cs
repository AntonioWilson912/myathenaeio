namespace MyAthenaeio.Models.ViewModels
{
    public class BookCopyIncludeOptions
    {
        public bool IncludeAuthors { get; set; }
        public bool IncludeBook { get; set; }
        public bool IncludeLoans { get; set; }

        public bool ForceReload { get; set; }

        public static BookCopyIncludeOptions Default => new()
        {
            IncludeAuthors = true,
            IncludeBook = true
        };

        public static BookCopyIncludeOptions None => new();

        public static BookCopyIncludeOptions WithAuthors => new()
        {
            IncludeAuthors = true
        };

        public static BookCopyIncludeOptions WithLoans => new()
        {
            IncludeLoans = true
        };

        public static BookCopyIncludeOptions Minimal => new()
        {
            IncludeBook = true
        };


        public static BookCopyIncludeOptions Full => new()
        {
            IncludeAuthors = true,
            IncludeBook = true,
            IncludeLoans = true
        };
    }
}
