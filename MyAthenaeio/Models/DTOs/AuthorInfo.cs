namespace MyAthenaeio.Models.DTOs
{
    public class AuthorInfo
    {
        public string Name { get; set; } = string.Empty;
        public string OpenLibraryKey { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
