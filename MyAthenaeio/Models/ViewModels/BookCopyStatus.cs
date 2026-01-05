namespace MyAthenaeio.Models.ViewModels
{
    public class BookCopyStatus
    {
        public int CopyId { get; set; }
        public string CopyNumber { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
        public string? BorrowerName { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
