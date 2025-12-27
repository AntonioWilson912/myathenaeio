using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public class BookCopyRepository : Repository<BookCopy>, IBookCopyRepository
    {
        public BookCopyRepository(AppDbContext context) : base(context) { }

        #region Query Methods

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

        public async Task<BookCopy> CreateCopyAsync(int bookId, string? condition = null, string? notes = null)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            // Get next copy number
            var existingCopies = await CountAsync(bc => bc.BookId == bookId);
            var copyNumber = $"Copy {existingCopies + 1}";

            var bookCopy = new BookCopy
            {
                BookId = bookId,
                CopyNumber = copyNumber,
                AcquisitionDate = DateTime.UtcNow,
                IsAvailable = true,
                Notes = notes
            };

            await AddAsync(bookCopy);
            return bookCopy;
        }

        public async Task MarkAsUnavailableAsync(int bookCopyId)
        {
            var copy = await GetByIdAsync(bookCopyId);
            if (copy == null)
                throw new InvalidOperationException("Book copy does not exist.");

            copy.IsAvailable = false;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsAvailableAsync(int bookCopyId)
        {
            var copy = await GetByIdAsync(bookCopyId);
            if (copy == null)
                throw new InvalidOperationException("Book copy does not exist.");

            copy.IsAvailable = true;
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(BookCopy bookCopy)
        {
            var existing = await _context.BookCopies.FindAsync(bookCopy.Id);
            if (existing == null)
                throw new InvalidOperationException("Book copy does not exist.");

            existing.CopyNumber = bookCopy.CopyNumber;
            existing.Notes = bookCopy.Notes;
            existing.IsAvailable = bookCopy.IsAvailable;

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}