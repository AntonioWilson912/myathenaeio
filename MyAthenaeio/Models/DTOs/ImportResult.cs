namespace MyAthenaeio.Models.DTOs
{
    public class ImportResult
    {
        public int BooksImported { get; set; }
        public int AuthorsImported { get; set; }
        public int GenresImported { get; set; }
        public int TagsImported { get; set; }
        public int CollectionsImported { get; set; }
        public int BorrowersImported { get; set; }
        public int CopiesImported { get; set; }
        public int LoansImported { get; set; }
        public int RenewalsImported { get; set; }
        public int ItemsSkipped { get; set; }
        public List<string> Errors { get; set; } = [];
        public bool Success => Errors.Count == 0;
    }
}
