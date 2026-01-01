using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyAthenaeio.Models.Entities
{
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Book> Books { get; set; } = [];

        [NotMapped]
        public int BookCount { get; set; }
    }
}
