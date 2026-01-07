namespace MyAthenaeio.Models.DTOs
{
    public class LibraryExportDTO
    {
        public DateTime ExportDate { get; set; }
        public string ExportVersion { get; set; } = "1.0";
        public List<BookExportDTO> Books { get; set; } = [];
        public List<AuthorExportDTO> Authors { get; set; } = [];
        public List<GenreExportDTO> Genres { get; set; } = [];
        public List<TagExportDTO> Tags { get; set; } = [];
        public List<CollectionExportDTO> Collections { get; set; } = [];
        public List<BorrowerExportDTO> Borrowers { get; set; } = [];
        public List<BookCopyExportDTO> BookCopies { get; set; } = [];
        public List<LoanExportDTO> Loans { get; set; } = [];
        public List<RenewalExportDTO> Renewals { get; set; } = [];
        public List<BookAuthorExportDTO> BookAuthors { get; set; } = [];
        public List<BookGenreExportDTO> BookGenres { get; set; } = [];
        public List<BookTagExportDTO> BookTags { get; set; } = [];
        public List<BookCollectionExportDTO> BookCollections { get; set; } = [];

        // Statistics for verification
        public ExportStatistics Statistics { get; set; } = new();
    }
}
