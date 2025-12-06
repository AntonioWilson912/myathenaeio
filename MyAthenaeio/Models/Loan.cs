using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models
{
    public class Loan
    {
        public int Id { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public string? Notes { get; set; }

        [Required]
        public Book Book { get; set; } = default!;
        public int BookId { get; set; }

        [Required]
        public Borrower Borrower { get; set; } = default!;

        public int BorrowerId { get; set; }
        public List<Renewal> Renewals { get; set; } = [];
    }
}
