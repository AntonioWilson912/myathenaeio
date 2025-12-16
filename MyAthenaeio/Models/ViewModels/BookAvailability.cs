namespace MyAthenaeio.Models.ViewModels
{
    public class BookAvailability
    {
        public bool BookExists { get; set; }
        public int TotalCopies { get; set; }
        public int OnLoan { get; set; }
        public int Available { get; set; }
        public bool IsAvailable => Available > 0;
    }
}
