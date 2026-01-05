using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyAthenaeio.Models.Entities
{
    public class Collection
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }
        public string? Notes { get; set; }

        public ICollection<Book> Books { get; set; } = [];

        [NotMapped]
        public int BookCount { get; set; }
    }
}
