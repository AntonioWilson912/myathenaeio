using System.Windows.Media.Imaging;

namespace MyAthenaeio.Data
{
    public class BookApiResponse
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public List<Models.Author> Authors { get; set; } = default!;
        public DateTime PublishDate { get; set; }
        public string? Isbn10 { get; set; }
        public string? Isbn13 { get; set; }
        public string Key { get; set; } = string.Empty;
        public BitmapImage? Cover { get; set; }
    }
}
