namespace MyAthenaeio.Models.DTOs
{
    public class BorrowerExportDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }
}
