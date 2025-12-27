using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models.Entities
{
    public class Loan
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int BookCopyId { get; set; }
        public int BorrowerId { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? Notes { get; set; }

        [Required]
        public Book Book { get; set; } = null!;

        [Required]
        public BookCopy BookCopy { get; set; } = null!;

        [Required]
        public Borrower Borrower { get; set; } = null!;

        public List<Renewal> Renewals { get; set; } = [];
    }
}
