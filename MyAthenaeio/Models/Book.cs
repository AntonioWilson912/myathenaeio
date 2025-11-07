using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Models
{
    internal class Book
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public List<string> Authors { get; set; } = default!;
        public DateTime PublishDate { get; set; }
        public string? Isbn10 { get; set; }
        public string? Isbn13 { get; set; }
        public string Key { get; set; } = string.Empty;
        public BitmapImage? Cover { get; set; }

        // For a future version of the app
        public string Location { get; set; } = string.Empty;
    }
}
