using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models.Entities
{
    public class Renewal
    {
        public int Id { get; set; }
        public DateTime RenewalDate { get; set; }
        public DateTime NewDueDate { get; set; }
        public string? Notes { get; set; }

        [Required]
        public Loan Loan { get; set; } = default!;
        public int LoanId { get; set; }
    }
}
