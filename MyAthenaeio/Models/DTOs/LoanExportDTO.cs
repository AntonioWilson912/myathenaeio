namespace MyAthenaeio.Models.DTOs
{
    public class LoanExportDTO
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int BookCopyId { get; set; }
        public int BorrowerId { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime EffectiveDueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int MaxRenewalsAllowed { get; set; }
        public int LoanPeriodDays { get; set; }
        public string? Notes { get; set; }
    }
}
