using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Data.Repositories
{
    public class BorrowerRepository(AppDbContext context) : Repository<Borrower>(context), IBorrowerRepository
    {

        #region Query Methods with Include Options

        public async Task<Borrower?> GetByIdAsync(int id, BorrowerIncludeOptions? options = null)
        {
            options ??= BorrowerIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Borrower?> GetByNameAsync(string name, BorrowerIncludeOptions? options = null)
        {
            options ??= BorrowerIncludeOptions.Default;
            if (options.ForceReload)
                DetachAll();
            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.FirstOrDefaultAsync(b => b.Name.ToLower() == name.ToLower());
        }

        public override async Task<List<Borrower>> GetAllAsync()
        {
            return await GetAllAsync(BorrowerIncludeOptions.Default);
        }

        public async Task<List<Borrower>> GetAllAsync(BorrowerIncludeOptions? options = null)
        {
            options ??= BorrowerIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.OrderBy(b => b.Name).ToListAsync();
        }

        public override async Task<List<Borrower>> GetAllAsNoTrackingAsync()
        {
            return await GetAllAsNoTrackingAsync(BorrowerIncludeOptions.Default);
        }

        public async Task<List<Borrower>> GetAllAsNoTrackingAsync(BorrowerIncludeOptions? options = null)
        {
            options ??= BorrowerIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable().AsNoTracking(), options);
            return await query.OrderBy(b => b.Name).ToListAsync();
        }

        public async Task<List<Borrower>> GetAllActiveAsync(BorrowerIncludeOptions? options = null)
        {
            options ??= BorrowerIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        }

        public async Task<List<Borrower>> SearchAsync(string query, BorrowerIncludeOptions? options = null)
        {
            options ??= BorrowerIncludeOptions.Default;
            query = query.ToLower();

            var borrowersQuery = BuildQuery(_dbSet.AsQueryable(), options);

            return await borrowersQuery
                .Where(b => b.Name.ToLower().Contains(query) ||
                            (b.Email != null && b.Email.ToLower().Contains(query)) ||
                            (b.Phone != null && b.Phone.Contains(query)))
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Borrower?> GetByEmailAsync(string email)
        {
            return await FirstOrDefaultAsync(b => b.Email == email);
        }

        public async Task<Borrower?> GetByPhoneAsync(string phone)
        {
            return await FirstOrDefaultAsync(b => b.Phone == phone);
        }

        #endregion

        #region Loan-Related Queries

        public async Task<List<Loan>> GetLoansByBorrowerAsync(int borrowerId)
        {
            return await _context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Renewals)
                .Where(l => l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetActiveLoansByBorrowerAsync(int borrowerId)
        {
            return await _context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Renewals)
                .Where(l => l.BorrowerId == borrowerId && l.ReturnDate == null)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public async Task<bool> HasActiveLoansAsync(int borrowerId)
        {
            return await _context.Loans
                .AnyAsync(l => l.BorrowerId == borrowerId && l.ReturnDate == null);
        }

        public async Task<int> GetActiveLoanCountAsync(int borrowerId)
        {
            return await _context.Loans
                .CountAsync(l => l.BorrowerId == borrowerId && l.ReturnDate == null);
        }

        #endregion

        #region Add Methods with Validation

        public override async Task<Borrower> AddAsync(Borrower borrower)
        {
            // Validation
            ValidateBorrower(borrower);

            // Check for duplicate email
            if (!string.IsNullOrEmpty(borrower.Email))
            {
                var emailExists = await AnyAsync(b => b.Email != null &&
                    b.Email.ToLower() == borrower.Email.ToLower());

                if (emailExists)
                    throw new InvalidOperationException(
                        $"A borrower with email '{borrower.Email}' already exists.");
            }

            // Check for duplicate phone
            if (!string.IsNullOrEmpty(borrower.Phone))
            {
                var phoneExists = await AnyAsync(b => b.Phone == borrower.Phone);

                if (phoneExists)
                    throw new InvalidOperationException(
                        $"A borrower with phone '{borrower.Phone}' already exists.");
            }

            // Set defaults
            if (borrower.DateAdded == default)
                borrower.DateAdded = DateTime.UtcNow;

            return await base.AddAsync(borrower);
        }

        private static void ValidateBorrower(Borrower borrower)
        {
            ArgumentNullException.ThrowIfNull(borrower);

            if (string.IsNullOrWhiteSpace(borrower.Name))
                throw new ArgumentException("Borrower name is required.", nameof(borrower));

            if (borrower.Name.Length > 200)
                throw new ArgumentException("Borrower name cannot exceed 200 characters.", nameof(borrower));

            if (borrower.Email != null && borrower.Email.Length > 200)
                throw new ArgumentException("Email cannot exceed 200 characters.", nameof(borrower));

            if (borrower.Phone != null && borrower.Phone.Length > 50)
                throw new ArgumentException("Phone cannot exceed 50 characters.", nameof(borrower));
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Borrower borrower)
        {
            // Validation
            ValidateBorrower(borrower);

            var existingBorrower = await _context.Borrowers.FindAsync(borrower.Id) ?? throw new InvalidOperationException("Borrower does not exist.");

            // Check for duplicate email (excluding current borrower)
            if (!string.IsNullOrEmpty(borrower.Email))
            {
                var emailExists = await AnyAsync(b =>
                    b.Id != borrower.Id &&
                    b.Email != null &&
                    b.Email.ToLower() == borrower.Email.ToLower());

                if (emailExists)
                    throw new InvalidOperationException(
                        $"A borrower with email '{borrower.Email}' already exists.");
            }

            // Check for duplicate phone (excluding current borrower)
            if (!string.IsNullOrEmpty(borrower.Phone))
            {
                var phoneExists = await AnyAsync(b =>
                    b.Id != borrower.Id &&
                    b.Phone == borrower.Phone);

                if (phoneExists)
                    throw new InvalidOperationException(
                        $"A borrower with phone '{borrower.Phone}' already exists.");
            }

            existingBorrower.Name = borrower.Name;
            existingBorrower.Email = borrower.Email;
            existingBorrower.Phone = borrower.Phone;
            existingBorrower.Notes = borrower.Notes;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            var borrower = await GetByIdAsync(id) ?? throw new InvalidOperationException("Borrower does not exist.");

            // Prevent deletion if they have any loan history
            var hasLoans = await _context.Loans
                .AnyAsync(l => l.BorrowerId == id);

            if (hasLoans)
                throw new InvalidOperationException(
                    $"Cannot delete borrower '{borrower.Name}' because they have loan history. " +
                    "Deleting borrowers with loan records would break historical data.");

            await base.DeleteAsync(borrower);
        }

        #endregion

        #region Helper Methods

        private static IQueryable<Borrower> BuildQuery(IQueryable<Borrower> query, BorrowerIncludeOptions options)
        {
            if (options.IncludeLoans)
            {
                query = query.Include(b => b.Loans)
                            .ThenInclude(l => l.Book);
            }

            return query;
        }

        #endregion
    }
}