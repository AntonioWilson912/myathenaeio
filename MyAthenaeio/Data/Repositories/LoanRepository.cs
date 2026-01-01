using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;

namespace MyAthenaeio.Data.Repositories
{
    public class LoanRepository : Repository<Loan>, ILoanRepository
    {
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
            // Check if book copy exists and load the book
            var bookCopy = await _context.BookCopies
                .Include(bc => bc.Book)
                .FirstOrDefaultAsync(bc => bc.Id == bookCopyId)
                ?? throw new InvalidOperationException("Book copy does not exist.");

            // Check if book copy is available
            if (!bookCopy.IsAvailable)
                throw new InvalidOperationException("This copy is currently unavailable.");

            // Check if borrower exists
            var borrower = await _context.Borrowers.FindAsync(borrowerId)
                ?? throw new InvalidOperationException("Borrower does not exist.");

            // Check if borrower already has an active loan for this book
            var hasActiveLoan = await _context.Loans
                .AnyAsync(l => l.BorrowerId == borrowerId
                            && l.BookId == bookCopy.BookId
                            && !l.ReturnDate.HasValue);

            if (hasActiveLoan)
                throw new InvalidOperationException("Borrower already has an active loan for this book.");

            // Create loan
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

            // Mark copy as unavailable
            bookCopy.IsAvailable = false;

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            return loan;
        }

        public async Task<Loan> ReturnAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.BookCopy)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == loanId)
                ?? throw new InvalidOperationException("Loan does not exist.");

            if (loan.ReturnDate != null)
                throw new InvalidOperationException("This book has already been returned.");

            loan.ReturnDate = DateTime.UtcNow;
            loan.BookCopy.IsAvailable = true;

            await _context.SaveChangesAsync();

            return loan;
        }

        public async Task<Renewal> RenewAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Renewals)
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == loanId)
                ?? throw new InvalidOperationException("Loan does not exist.");

            if (loan.ReturnDate != null)
                throw new InvalidOperationException("Cannot renew a returned loan.");

            if (loan.RenewalsRemaining == 0)
                throw new InvalidOperationException($"Maximum renewals ({loan.MaxRenewalsAllowed}) reached for this loan.");

            var renewal = new Renewal
            {
                LoanId = loanId,
                RenewalDate = DateTime.UtcNow,
                NewDueDate = loan.GetEffectiveDueDate().AddDays(loan.LoanPeriodDays)
            };

            _context.Renewals.Add(renewal);
            await _context.SaveChangesAsync();

            return renewal;
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