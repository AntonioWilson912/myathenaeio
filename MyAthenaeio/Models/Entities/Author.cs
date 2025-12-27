using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models.Entities
{
    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OpenLibraryKey { get; set; }
        public string? Bio { get; set; }

        public ICollection<Book> Books { get; set; } = [];
    }
}
