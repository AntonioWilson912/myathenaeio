using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models.Entities
{
    public class Tag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public ICollection<Book> Books { get; set; } = [];
    }
}
