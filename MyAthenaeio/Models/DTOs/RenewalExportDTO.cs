namespace MyAthenaeio.Models.DTOs
{
    public class RenewalExportDTO
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public DateTime RenewalDate { get; set; }
        public DateTime OldDueDate { get; set; }
        public DateTime NewDueDate { get; set; }
        public string? Notes { get; set; }
    }
}
