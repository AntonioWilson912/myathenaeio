namespace MyAthenaeio.Models.DTOs
{
    public class BookCopyExportDTO
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string CopyNumber { get; set; } = string.Empty;
        public DateTime AcquisitionDate { get; set; }
        public bool IsAvailable { get; set; }
        public string? Condition { get; set; }
        public string? Notes { get; set; }
    }
}
