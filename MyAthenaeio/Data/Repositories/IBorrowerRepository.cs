using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Data.Repositories
{
    public interface IBorrowerRepository : IRepository<Borrower>
    {
        // Override base methods with include options
        Task<Borrower?> GetByIdAsync(int id, BorrowerIncludeOptions? options = null);
        Task<Borrower?> GetByNameAsync(string name, BorrowerIncludeOptions? options = null);
        Task<List<Borrower>> GetAllAsync(BorrowerIncludeOptions? options = null);
        Task<List<Borrower>> GetAllAsNoTrackingAsync(BorrowerIncludeOptions? options = null);
        Task<List<Borrower>> GetAllActiveAsync(BorrowerIncludeOptions? options = null);

        // Borrower-specific queries
        Task<List<Borrower>> SearchAsync(string query, BorrowerIncludeOptions? options = null);
        Task<Borrower?> GetByEmailAsync(string email);
        Task<Borrower?> GetByPhoneAsync(string phone);

        // Loan-related queries
        Task<List<Loan>> GetLoansByBorrowerAsync(int borrowerId);
        Task<List<Loan>> GetActiveLoansByBorrowerAsync(int borrowerId);
        Task<bool> HasActiveLoansAsync(int borrowerId);
        Task<int> GetActiveLoanCountAsync(int borrowerId);
    }
}