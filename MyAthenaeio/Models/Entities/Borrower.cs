using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models.Entities
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

        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime DateAdded { get; set; }

        // Navigation property
        public ICollection<Loan> Loans { get; set; } = [];

        // Computed properties
        public int ActiveLoansCount => Loans?.Count(l => !l.IsReturned) ?? 0;
        public int TotalLoansCount => Loans?.Count ?? 0;
        public int OverdueLoansCount => Loans?.Count(l => l.IsOverdue) ?? 0;
        public bool HasOverdueLoans => OverdueLoansCount > 0;

        public IEnumerable<Loan> GetOverdueLoans()
        {
            return Loans?.Where(l => l.IsOverdue) ?? Enumerable.Empty<Loan>();
        }
    }
}
