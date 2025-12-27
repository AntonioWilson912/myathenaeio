namespace MyAthenaeio.Models.ViewModels
{
    public class BookIncludeOptions
    {
        public bool IncludeAuthors { get; set; }
        public bool IncludeGenres { get; set; }
        public bool IncludeTags { get; set; }
        public bool IncludeCollections { get; set; }
        public bool ForceReload { get; set; }

        public static BookIncludeOptions Default => new()
        {
            IncludeAuthors = true,
            IncludeGenres = true,
            IncludeTags = true,
            IncludeCollections = true
        };

        public static BookIncludeOptions None => new();

        public static BookIncludeOptions AuthorsOnly => new()
        {
            IncludeAuthors = true
        };

        public static BookIncludeOptions Minimal => new()
        {
            IncludeAuthors = true,
            IncludeGenres = true
        };
    }
}