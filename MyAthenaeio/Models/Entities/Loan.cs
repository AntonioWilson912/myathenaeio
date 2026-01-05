using MyAthenaeio.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int MaxRenewalsAllowed { get; set; } = 2;
        public int LoanPeriodDays { get; set; } = 14;

        [Required]
        public Book Book { get; set; } = null!;

        [Required]
        public BookCopy BookCopy { get; set; } = null!;

        [Required]
        public Borrower Borrower { get; set; } = null!;

        public DateTime EffectiveDueDate => this.GetEffectiveDueDate();

        public int RenewalsRemaining => Math.Max(0, MaxRenewalsAllowed - RenewalCount);
        public int RenewalCount => Renewals.Count;
        public bool IsReturned => ReturnDate.HasValue;
        public bool IsOverdue => !IsReturned && DateTime.Now.Date > this.GetEffectiveDueDate().Date;

        public ICollection<Renewal> Renewals { get; set; } = [];
    }
}
