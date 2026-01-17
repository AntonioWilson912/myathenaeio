using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Utils;
using Serilog;

namespace MyAthenaeio.Data.Repositories
{
    public class LoanRepository : Repository<Loan>, ILoanRepository
    {
        private static readonly ILogger _logger = Log.ForContext<LoanRepository>();

        public LoanRepository(AppDbContext context) : base(context) { }

        #region Query Methods with Include Options

        public async Task<Loan?> GetByIdAsync(int id, LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.FirstOrDefaultAsync(l => l.Id == id);
        }

        public override async Task<List<Loan>> GetAllAsync()
        {
            return await GetAllAsync(LoanIncludeOptions.Default);
        }

        public async Task<List<Loan>> GetAllAsync(LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.OrderByDescending(l => l.CheckoutDate).ToListAsync();
        }

        public override async Task<List<Loan>> GetAllAsNoTrackingAsync()
        {
            return await GetAllAsNoTrackingAsync(LoanIncludeOptions.Default);
        }

        public async Task<List<Loan>> GetAllAsNoTrackingAsync(LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable().AsNoTracking(), options);
            return await query.OrderByDescending(l => l.CheckoutDate).ToListAsync();
        }

        public async Task<List<Loan>> GetActiveLoansAsync(LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(l => l.ReturnDate == null)
                .OrderBy(l => l.DueDate)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetOverdueLoansAsync(LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;
            var today = DateTime.Today;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            var loans = await query
                .Where(l => l.ReturnDate == null)
                .Include(l => l.Renewals)
                .ToListAsync();

            return [.. loans
                .Where(l => l.GetEffectiveDueDate() < today)
                .OrderBy(l => l.GetEffectiveDueDate())];
        }

        public async Task<List<Loan>> GetDueSoonAsync(int daysAhead = 7, LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;
            var today = DateTime.Today;
            var targetDate = today.AddDays(daysAhead);

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            var loans = await query
                .Where(l => l.ReturnDate == null)
                .Include(l => l.Renewals)
                .ToListAsync();

            return [.. loans
                .Where(l =>
                {
                    var dueDate = l.GetEffectiveDueDate();
                    return dueDate >= today && dueDate <= targetDate;
                })
                .OrderBy(l => l.GetEffectiveDueDate())];
        }

        public async Task<List<Loan>> GetByBookAsync(int bookId, LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(l => l.BookId == bookId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetByBorrowerAsync(int borrowerId, LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(l => l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetActiveLoansByBorrowerAsync(int borrowerId, LoanIncludeOptions? options = null)
        {
            options ??= LoanIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(l => l.BorrowerId == borrowerId && l.ReturnDate == null)
                .OrderBy(l => l.DueDate)
                .ToListAsync();
        }

        #endregion

        #region Loan Operations

        public async Task<Loan> CheckoutAsync(int bookCopyId, int borrowerId, int maxRenewals, int loanPeriodDays)
        {
            try
            {
                _logger.Debug("Checkout requested: BookCopy {BookCopyId}, Borrower {BorrowerId}",
                    bookCopyId, borrowerId);

                var bookCopy = await _context.BookCopies
                    .Include(bc => bc.Book)
                    .FirstOrDefaultAsync(bc => bc.Id == bookCopyId);

                if (bookCopy == null)
                {
                    _logger.Warning("Checkout failed: BookCopy {BookCopyId} not found", bookCopyId);
                    throw new InvalidOperationException("Book copy does not exist.");
                }

                if (!bookCopy.IsAvailable)
                {
                    _logger.Warning("Checkout failed: BookCopy {BookCopyId} unavailable", bookCopyId);
                    throw new InvalidOperationException("This copy is currently unavailable.");
                }

                var borrower = await _context.Borrowers.FindAsync(borrowerId);
                if (borrower == null)
                {
                    _logger.Warning("Checkout failed: Borrower {BorrowerId} not found", borrowerId);
                    throw new InvalidOperationException("Borrower does not exist.");
                }

                var hasActiveLoan = await _context.Loans
                    .AnyAsync(l => l.BorrowerId == borrowerId
                                && l.BookId == bookCopy.BookId
                                && !l.ReturnDate.HasValue);

                if (hasActiveLoan)
                {
                    _logger.Warning("Checkout failed: Borrower {BorrowerId} already has active loan for Book {BookId}",
                        borrowerId, bookCopy.BookId);
                    throw new InvalidOperationException(
                        "Borrower already has an active loan for this book.");
                }

                var loan = new Loan
                {
                    BookId = bookCopy.BookId,
                    BookCopyId = bookCopy.Id,
                    BorrowerId = borrowerId,
                    CheckoutDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(loanPeriodDays),
                    MaxRenewalsAllowed = maxRenewals,
                    LoanPeriodDays = loanPeriodDays,
                    Book = bookCopy.Book,
                    BookCopy = bookCopy,
                    Borrower = borrower
                };

                bookCopy.IsAvailable = false;

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();

                _logger.Information("Book checked out: {BookTitle} to {BorrowerName} (Loan ID: {LoanId}, Due: {DueDate})",
                    bookCopy.Book.Title, borrower.Name, loan.Id, loan.DueDate.ToString("yyyy-MM-dd"));

                return loan;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Checkout failed for BookCopy {BookCopyId}, Borrower {BorrowerId}",
                    bookCopyId, borrowerId);
                throw;
            }
        }

        public async Task<Loan> ReturnAsync(int loanId)
        {
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.Book)
                    .Include(l => l.BookCopy)
                    .Include(l => l.Borrower)
                    .FirstOrDefaultAsync(l => l.Id == loanId);

                if (loan == null)
                {
                    _logger.Warning("Return failed: Loan {LoanId} not found", loanId);
                    throw new InvalidOperationException("Loan does not exist.");
                }

                if (loan.ReturnDate != null)
                {
                    _logger.Warning("Return failed: Loan {LoanId} already returned on {ReturnDate}",
                        loanId, loan.ReturnDate);
                    throw new InvalidOperationException("This book has already been returned.");
                }

                loan.ReturnDate = DateTime.UtcNow;
                loan.BookCopy.IsAvailable = true;

                await _context.SaveChangesAsync();

                var isLate = loan.ReturnDate > loan.GetEffectiveDueDate();
                _logger.Information("Book returned: {BookTitle} from {BorrowerName} (Loan ID: {LoanId}, {Status})",
                    loan.Book.Title, loan.Borrower.Name, loanId,
                    isLate ? $"LATE by {(loan.ReturnDate.Value - loan.GetEffectiveDueDate()).Days} days" : "On time");

                return loan;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Return failed for Loan {LoanId}", loanId);
                throw;
            }
        }

        public async Task<Renewal> AddRenewalAsync(Renewal renewal)
        {
            var loan = await _context.Loans
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.Id == renewal.LoanId)
                ?? throw new InvalidOperationException("Loan does not exist.");
            if (loan.ReturnDate != null)
                throw new InvalidOperationException("Cannot renew a returned loan.");
            if (loan.RenewalsRemaining == 0)
                throw new InvalidOperationException($"Maximum renewals ({loan.MaxRenewalsAllowed}) reached for this loan.");
            _context.Renewals.Add(renewal);
            await _context.SaveChangesAsync();
            return renewal;
        }

        public async Task<Renewal> RenewAsync(int loanId)
        {
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.Renewals)
                    .Include(l => l.Book)
                    .FirstOrDefaultAsync(l => l.Id == loanId);

                if (loan == null)
                {
                    _logger.Warning("Renewal failed: Loan {LoanId} not found", loanId);
                    throw new InvalidOperationException("Loan does not exist.");
                }

                if (loan.ReturnDate != null)
                {
                    _logger.Warning("Renewal failed: Loan {LoanId} already returned", loanId);
                    throw new InvalidOperationException("Cannot renew a returned loan.");
                }

                if (loan.RenewalsRemaining == 0)
                {
                    _logger.Warning("Renewal failed: Loan {LoanId} has no renewals remaining (max: {Max})",
                        loanId, loan.MaxRenewalsAllowed);
                    throw new InvalidOperationException(
                        $"Maximum renewals ({loan.MaxRenewalsAllowed}) reached for this loan.");
                }

                var renewal = new Renewal
                {
                    LoanId = loanId,
                    OldDueDate = loan.GetEffectiveDueDate(),
                    RenewalDate = DateTime.UtcNow,
                    NewDueDate = loan.GetEffectiveDueDate().AddDays(loan.LoanPeriodDays)
                };

                _context.Renewals.Add(renewal);
                await _context.SaveChangesAsync();

                _logger.Information("Loan renewed: {BookTitle} (Loan ID: {LoanId}, New due date: {NewDueDate}, Renewals remaining: {Remaining})",
                    loan.Book.Title, loanId, renewal.NewDueDate.ToString("yyyy-MM-dd"), loan.RenewalsRemaining - 1);

                return renewal;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Renewal failed for Loan {LoanId}", loanId);
                throw;
            }
        }

        #endregion

        #region Loan Calculations

        public async Task<DateTime> GetEffectiveDueDateAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.Id == loanId)
                ?? throw new InvalidOperationException("Loan does not exist.");

            return loan.GetEffectiveDueDate();
        }

        public async Task<bool> IsOverdueAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.Id == loanId)
                ?? throw new InvalidOperationException("Loan does not exist.");

            if (loan.ReturnDate != null)
                return false;

            return loan.GetEffectiveDueDate() < DateTime.Today;
        }

        public async Task<int> GetDaysOverdueAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.Id == loanId)
                ?? throw new InvalidOperationException("Loan does not exist.");

            if (loan.ReturnDate != null)
                return 0;

            var dueDate = loan.GetEffectiveDueDate();
            if (dueDate >= DateTime.Today)
                return 0;

            return (DateTime.Today - dueDate).Days;
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Loan loan)
        {
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Helper Methods

        private static IQueryable<Loan> BuildQuery(IQueryable<Loan> query, LoanIncludeOptions options)
        {
            if (options.IncludeBookCopy)
            {
                query = query.Include(l => l.BookCopy);

                if (options.IncludeBook)
                {
                    query = query.Include(l => l.Book);

                    if (options.IncludeBookAuthors)
                    {
                        query = query.Include(l => l.Book)
                                    .ThenInclude(b => b.Authors);
                    }
                }
            }

            if (options.IncludeBorrower)
            {
                query = query.Include(l => l.Borrower);
            }

            if (options.IncludeRenewals)
            {
                query = query.Include(l => l.Renewals);
            }

            return query;
        }

        #endregion
    }
}