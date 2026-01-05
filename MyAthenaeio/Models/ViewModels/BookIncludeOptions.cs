namespace MyAthenaeio.Models.ViewModels
{
    public class BookIncludeOptions
    {
        public bool IncludeAuthors { get; set; }
        public bool IncludeGenres { get; set; }
        public bool IncludeTags { get; set; }
        public bool IncludeCollections { get; set; }
        public bool IncludeCopies { get; set; }
        public bool IncludeLoans { get; set; }
        public bool IncludeBorrowers { get; set; }

        public bool ForceReload { get; set; }

        public static BookIncludeOptions Default => new()
        {
            IncludeAuthors = true,
            IncludeGenres = true,
            IncludeTags = true,
            IncludeCollections = true
        };

        public static BookIncludeOptions None => new();

        public static BookIncludeOptions WithAuthors => new()
        {
            IncludeAuthors = true
        };

        public static BookIncludeOptions WithGenres => new()
        {
            IncludeGenres = true
        };

        public static BookIncludeOptions WithTags => new()
        {
            IncludeTags = true
        };

        public static BookIncludeOptions WithCollections => new()
        {
            IncludeCollections = true
        };

        public static BookIncludeOptions WithCopies => new()
        {
            IncludeCopies = true
        };

        public static BookIncludeOptions WithLoans => new()
        {
            IncludeLoans = true
        };

        public static BookIncludeOptions WithBorrowers => new()
        {
            IncludeBorrowers = true
        };

        public static BookIncludeOptions Minimal => new()
        {
            IncludeAuthors = true,
            IncludeGenres = true
        };

        public static BookIncludeOptions Search => new()
        {
            IncludeAuthors = true,
            IncludeGenres = true,
            IncludeTags = true,
            IncludeCollections = true,
            IncludeCopies = true
        };

        public static BookIncludeOptions Full => new()
        {
            IncludeAuthors = true,
            IncludeGenres = true,
            IncludeTags = true,
            IncludeCollections = true,
            IncludeCopies = true,
            IncludeLoans = true,
            IncludeBorrowers = true
        };
    }
}