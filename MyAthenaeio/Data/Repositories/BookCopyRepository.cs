using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using Serilog;

namespace MyAthenaeio.Data.Repositories
{
    public class BookCopyRepository : Repository<BookCopy>, IBookCopyRepository
    {
        private static readonly ILogger _logger = Log.ForContext<BookCopyRepository>();

        public BookCopyRepository(AppDbContext context) : base(context) { }

        #region Query Methods

        public async Task<BookCopy?> GetByIdAsync(int bookCopyId, BookCopyIncludeOptions? options = null)
        {
            options ??= BookCopyIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query.FirstOrDefaultAsync(bc => bc.Id == bookCopyId);
        }

        public override async Task<List<BookCopy>> GetAllAsNoTrackingAsync()
        {
            return await GetAllAsNoTrackingAsync(BookCopyIncludeOptions.Default);
        }

        public async Task<List<BookCopy>> GetAllAsNoTrackingAsync(BookCopyIncludeOptions? options = null)
        {
            options ??= BookCopyIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable().AsNoTracking(), options);

            return await query.ToListAsync();
        }

        public async Task<List<BookCopy>> GetByBookAsync(int bookId)
        {
            return await _dbSet
                .Include(bc => bc.Book)
                .Where(bc => bc.BookId == bookId)
                .OrderBy(bc => bc.CopyNumber)
                .ToListAsync();
        }

        public async Task<List<BookCopy>> GetAvailableCopiesAsync(int bookId)
        {
            return await _dbSet
                .Include(bc => bc.Book)
                .Where(bc => bc.BookId == bookId && bc.IsAvailable)
                .OrderBy(bc => bc.CopyNumber)
                .ToListAsync();
        }

        public async Task<BookCopy?> GetByCopyNumberAsync(int bookId, string copyNumber)
        {
            return await FirstOrDefaultAsync(bc =>
                bc.BookId == bookId && bc.CopyNumber == copyNumber);
        }

        public async Task<int> GetAvailableCountAsync(int bookId)
        {
            return await CountAsync(bc => bc.BookId == bookId && bc.IsAvailable);
        }

        public async Task<bool> HasAvailableCopiesAsync(int bookId)
        {
            return await AnyAsync(bc => bc.BookId == bookId && bc.IsAvailable);
        }

        #endregion

        #region Loan-Related Methods

        public async Task<List<Loan>> GetLoanHistoryAsync(int bookCopyId)
        {
            return await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Borrower)
                .Include(l => l.Renewals)
                .Where(l => l.BookCopyId == bookCopyId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public async Task<Loan?> GetCurrentLoanAsync(int bookCopyId)
        {
            return await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Borrower)
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.BookCopyId == bookCopyId && l.ReturnDate == null);
        }

        #endregion

        #region Operations

        public async Task<BookCopy> AddCopyAsync(int bookId, BookCopy bookCopy)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                {
                    _logger.Warning("Cannot add copy: Book {BookId} not found", bookId);
                    throw new InvalidOperationException("Book does not exist.");
                }

                _logger.Debug("Adding copy {CopyNumber} for book {BookId}", bookCopy.CopyNumber, bookId);

                await AddAsync(bookCopy);

                _logger.Information("Book copy added: {CopyNumber} for book {BookTitle} (Copy ID: {CopyId})",
                    bookCopy.CopyNumber, book.Title, bookCopy.Id);

                return bookCopy;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add copy for book {BookId}", bookId);
                throw;
            }
        }

        public async Task MarkAsUnavailableAsync(int bookCopyId)
        {
            try
            {
                var copy = await GetByIdAsync(bookCopyId);
                if (copy == null)
                {
                    _logger.Warning("Cannot mark unavailable: BookCopy {CopyId} not found", bookCopyId);
                    throw new InvalidOperationException("Book copy does not exist.");
                }

                _logger.Debug("Marking book copy {CopyId} as unavailable", bookCopyId);

                copy.IsAvailable = false;
                await _context.SaveChangesAsync();

                _logger.Information("Book copy marked unavailable: {CopyId}", bookCopyId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to mark book copy {CopyId} as unavailable", bookCopyId);
                throw;
            }
        }

        public async Task MarkAsAvailableAsync(int bookCopyId)
        {
            try
            {
                var copy = await GetByIdAsync(bookCopyId);
                if (copy == null)
                {
                    _logger.Warning("Cannot mark available: BookCopy {CopyId} not found", bookCopyId);
                    throw new InvalidOperationException("Book copy does not exist.");
                }

                _logger.Debug("Marking book copy {CopyId} as available", bookCopyId);

                copy.IsAvailable = true;
                await _context.SaveChangesAsync();

                _logger.Information("Book copy marked available: {CopyId}", bookCopyId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to mark book copy {CopyId} as available", bookCopyId);
                throw;
            }
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(BookCopy bookCopy)
        {
            try
            {
                var existing = await _context.BookCopies.FindAsync(bookCopy.Id);
                if (existing == null)
                {
                    _logger.Warning("Update failed: BookCopy {Id} not found", bookCopy.Id);
                    throw new InvalidOperationException("Book copy does not exist.");
                }

                _logger.Debug("Updating book copy {Id}", bookCopy.Id);

                existing.CopyNumber = bookCopy.CopyNumber;
                existing.Notes = bookCopy.Notes;
                existing.Condition = bookCopy.Condition;
                existing.AcquisitionDate = bookCopy.AcquisitionDate;
                existing.IsAvailable = bookCopy.IsAvailable;

                await _context.SaveChangesAsync();

                _logger.Information("Book copy updated: {Id}", bookCopy.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update book copy {Id}", bookCopy.Id);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private static IQueryable<BookCopy> BuildQuery(IQueryable<BookCopy> query, BookCopyIncludeOptions options)
        {
            if (options.IncludeBook)
            {
                query = query.Include(bc => bc.Book);

                if (options.IncludeAuthors)
                {
                    query = query.Include(bc => bc.Book)
                                    .ThenInclude(b => b.Authors);
                }
            }

            if (options.IncludeLoans)
            {
                query = query.Include(bc => bc.Loans)
                                .ThenInclude(l => l.Renewals)
                             .Include(bc => bc.Loans)
                                .ThenInclude(l => l.Borrower);
            }

            return query;
        }

        #endregion
    }
}