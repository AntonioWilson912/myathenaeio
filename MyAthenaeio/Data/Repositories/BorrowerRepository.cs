using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using Serilog;

namespace MyAthenaeio.Data.Repositories
{
    public class BorrowerRepository(AppDbContext context) : Repository<Borrower>(context), IBorrowerRepository
    {
        private static readonly ILogger _logger = Log.ForContext<BorrowerRepository>();

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
            try
            {
                ValidateBorrower(borrower);

                _logger.Debug("Adding borrower: {Name}", borrower.Name);

                if (!string.IsNullOrEmpty(borrower.Email))
                {
                    var emailExists = await AnyAsync(b => b.Email != null &&
                        b.Email.ToLower() == borrower.Email.ToLower());

                    if (emailExists)
                    {
                        _logger.Warning("Cannot add borrower - duplicate email: {Email}", borrower.Email);
                        throw new InvalidOperationException(
                            $"A borrower with email '{borrower.Email}' already exists.");
                    }
                }

                if (!string.IsNullOrEmpty(borrower.Phone))
                {
                    var phoneExists = await AnyAsync(b => b.Phone == borrower.Phone);

                    if (phoneExists)
                    {
                        _logger.Warning("Cannot add borrower - duplicate phone: {Phone}", borrower.Phone);
                        throw new InvalidOperationException(
                            $"A borrower with phone '{borrower.Phone}' already exists.");
                    }
                }

                if (borrower.DateAdded == default)
                    borrower.DateAdded = DateTime.UtcNow;

                var result = await base.AddAsync(borrower);

                _logger.Information("Borrower added: {Name} (ID: {Id})", borrower.Name, borrower.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add borrower: {Name}", borrower.Name);
                throw;
            }
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
            try
            {
                ValidateBorrower(borrower);

                var existingBorrower = await _context.Borrowers.FindAsync(borrower.Id);
                if (existingBorrower == null)
                {
                    _logger.Warning("Update failed: Borrower {Id} not found", borrower.Id);
                    throw new InvalidOperationException("Borrower does not exist.");
                }

                _logger.Debug("Updating borrower: {Name} (ID: {Id})", borrower.Name, borrower.Id);

                if (!string.IsNullOrEmpty(borrower.Email))
                {
                    var emailExists = await AnyAsync(b =>
                        b.Id != borrower.Id &&
                        b.Email != null &&
                        b.Email.ToLower() == borrower.Email.ToLower());

                    if (emailExists)
                    {
                        _logger.Warning("Update failed: duplicate email {Email} for borrower {Id}",
                            borrower.Email, borrower.Id);
                        throw new InvalidOperationException(
                            $"A borrower with email '{borrower.Email}' already exists.");
                    }
                }

                if (!string.IsNullOrEmpty(borrower.Phone))
                {
                    var phoneExists = await AnyAsync(b =>
                        b.Id != borrower.Id &&
                        b.Phone == borrower.Phone);

                    if (phoneExists)
                    {
                        _logger.Warning("Update failed: duplicate phone {Phone} for borrower {Id}",
                            borrower.Phone, borrower.Id);
                        throw new InvalidOperationException(
                            $"A borrower with phone '{borrower.Phone}' already exists.");
                    }
                }

                existingBorrower.Name = borrower.Name;
                existingBorrower.Email = borrower.Email;
                existingBorrower.Phone = borrower.Phone;
                existingBorrower.Notes = borrower.Notes;

                await _context.SaveChangesAsync();

                _logger.Information("Borrower updated: {Name} (ID: {Id})", borrower.Name, borrower.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update borrower ID: {Id}", borrower.Id);
                throw;
            }
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            try
            {
                var borrower = await GetByIdAsync(id);
                if (borrower == null)
                {
                    _logger.Warning("Delete failed: Borrower {Id} not found", id);
                    throw new InvalidOperationException("Borrower does not exist.");
                }

                var hasLoans = await _context.Loans
                    .AnyAsync(l => l.BorrowerId == id);

                if (hasLoans)
                {
                    _logger.Warning("Cannot delete borrower {Id} '{Name}' - has loan history",
                        id, borrower.Name);
                    throw new InvalidOperationException(
                        $"Cannot delete borrower '{borrower.Name}' because they have loan history. " +
                        "Deleting borrowers with loan records would break historical data.");
                }

                await base.DeleteAsync(borrower);
                _logger.Information("Borrower deleted: {Name} (ID: {Id})", borrower.Name, id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete borrower ID: {Id}", id);
                throw;
            }
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