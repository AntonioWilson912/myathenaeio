namespace MyAthenaeio.Models.DTOs
{
    public class BookExportDTO
    {
        public int Id { get; set; }
        public string ISBN { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
