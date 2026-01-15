namespace MyAthenaeio.Models.DTOs
{
    public class ExportStatistics
    {
        public int TotalBooks { get; set; }
        public int TotalAuthors { get; set; }
        public int TotalGenres { get; set; }
        public int TotalTags { get; set; }
        public int TotalCollections { get; set; }
        public int TotalBorrowers { get; set; }
        public int TotalCopies { get; set; }
        public int ActiveLoans { get; set; }
        public int CompletedLoans { get; set; }
        public int TotalRenewals { get; set; }
    }
}
