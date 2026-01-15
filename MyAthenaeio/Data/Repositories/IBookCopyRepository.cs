using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Data.Repositories
{
    public interface IBookCopyRepository : IRepository<BookCopy>
    {
        // BookCopy-specific queries
        Task<BookCopy?> GetByIdAsync(int bookCopyId, BookCopyIncludeOptions? options = null);
        Task<List<BookCopy>> GetByBookAsync(int bookId);
        Task<List<BookCopy>> GetAllAsNoTrackingAsync(BookCopyIncludeOptions? options = null);
        Task<List<BookCopy>> GetAvailableCopiesAsync(int bookId);
        Task<BookCopy?> GetByCopyNumberAsync(int bookId, string copyNumber);
        Task<int> GetAvailableCountAsync(int bookId);
        Task<bool> HasAvailableCopiesAsync(int bookId);

        // Loan-related
        Task<List<Loan>> GetLoanHistoryAsync(int bookCopyId);
        Task<Loan?> GetCurrentLoanAsync(int bookCopyId);

        // Operations
        Task<BookCopy> AddCopyAsync(int bookId, BookCopy copy);
        Task MarkAsUnavailableAsync(int bookCopyId);
        Task MarkAsAvailableAsync(int bookCopyId);
    }
}