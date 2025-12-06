using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models
{
    public class Borrower
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }
        public DateTime DateAdded { get; set; }

        public List<Loan> Loans { get; set; } = [];
    }
}
