namespace MyAthenaeio.Models.ViewModels
{
    public class BookAvailability
    {
        public bool BookExists { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int OnLoan => TotalCopies - AvailableCopies;
        public bool IsAvailable => AvailableCopies > 0;

        public List<BookCopyStatus> CopyStatuses { get; set; } = [];
    }
}
