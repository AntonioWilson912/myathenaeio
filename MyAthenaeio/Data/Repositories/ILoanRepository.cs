using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Data.Repositories
{
    public interface ILoanRepository : IRepository<Loan>
    {
        // Override base methods with include options
        Task<Loan?> GetByIdAsync(int id, LoanIncludeOptions? options = null);
        Task<List<Loan>> GetAllAsync(LoanIncludeOptions? options = null);

        // Loan-specific queries
        Task<List<Loan>> GetActiveLoansAsync(LoanIncludeOptions? options = null);
        Task<List<Loan>> GetOverdueLoansAsync(LoanIncludeOptions? options = null);
        Task<List<Loan>> GetDueSoonAsync(int daysAhead = 7, LoanIncludeOptions? options = null);
        Task<List<Loan>> GetByBookAsync(int bookId, LoanIncludeOptions? options = null);
        Task<List<Loan>> GetByBorrowerAsync(int borrowerId, LoanIncludeOptions? options = null);
        Task<List<Loan>> GetActiveLoansByBorrowerAsync(int borrowerId, LoanIncludeOptions? options = null);

        // Loan operations
        Task<Loan> CheckoutAsync(int bookId, int borrowerId, int loanPeriodDays = 14);
        Task<Loan> ReturnAsync(int loanId);
        Task<Renewal> RenewAsync(int loanId, int additionalDays = 14, int maxRenewals = 3);

        // Loan calculations
        Task<DateTime> GetEffectiveDueDateAsync(int loanId);
        Task<bool> IsOverdueAsync(int loanId);
        Task<int> GetDaysOverdueAsync(int loanId);
    }
}