namespace MyAthenaeio.Models.DTOs
{
    public class AuthorExportDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OpenLibraryKey { get; set; }
        public string? Bio { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
