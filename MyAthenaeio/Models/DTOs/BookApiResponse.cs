using System.Windows.Media.Imaging;

namespace MyAthenaeio.Models.DTOs
{
    public class BookApiResponse
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public List<AuthorInfo> Authors { get; set; } = new();
        public string? Publisher { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? Isbn10 { get; set; }
        public string? Isbn13 { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public BitmapImage? Cover { get; set; }
    }
}
