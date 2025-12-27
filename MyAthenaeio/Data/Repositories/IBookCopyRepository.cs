using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public interface IBookCopyRepository : IRepository<BookCopy>
    {
        // BookCopy-specific queries
        Task<List<BookCopy>> GetByBookAsync(int bookId);
        Task<List<BookCopy>> GetAvailableCopiesAsync(int bookId);
        Task<BookCopy?> GetByCopyNumberAsync(int bookId, string copyNumber);
        Task<int> GetAvailableCountAsync(int bookId);
        Task<bool> HasAvailableCopiesAsync(int bookId);

        // Loan-related
        Task<List<Loan>> GetLoanHistoryAsync(int bookCopyId);
        Task<Loan?> GetCurrentLoanAsync(int bookCopyId);

        // Operations
        Task<BookCopy> CreateCopyAsync(int bookId, string? condition = null, string? notes = null);
        Task MarkAsUnavailableAsync(int bookCopyId);
        Task MarkAsAvailableAsync(int bookCopyId);
    }
}