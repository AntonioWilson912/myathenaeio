using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models
{
    public class Collection
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public ICollection<Book> Books { get; set; } = [];
    }
}
