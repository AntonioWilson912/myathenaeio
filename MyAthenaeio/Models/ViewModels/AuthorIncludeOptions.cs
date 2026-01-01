namespace MyAthenaeio.Models.ViewModels
{
    public class AuthorIncludeOptions
    {
        public bool IncludeBooks { get; set; }
        public bool ForceReload { get; set; }

        public static AuthorIncludeOptions Default => new()
        {
            IncludeBooks = true
        };

        public static AuthorIncludeOptions None => new();

        public static AuthorIncludeOptions WithBooks => new()
        {
            IncludeBooks = true
        };
    }
}