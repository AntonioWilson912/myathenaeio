using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? OpenLibraryKey { get; set; }
        public string? Bio { get; set; }


        public ICollection<Book> Books { get; set; } = [];
    }
}
