using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Data;
using MyAthenaeio.Models;
using MyAthenaeio.Utils;

namespace MyAthenaeio.Services
{
    public static class LibraryService
    {
        #region Authors

        #endregion

        #region Books

        public static async Task<List<Book>> GetAllBooksAsync()
        {
            using var context = new AppDbContext();
            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public static async Task<List<Book>> GetBooksByAuthorAsync(Author author)
        {
            return await GetBooksByAuthorAsync(author.Id);
        }

        public static async Task<List<Book>> GetBooksByAuthorAsync(int authorId)
        {
            using var context = new AppDbContext();
            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(book => book.Authors.Any(a => a.Id == authorId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public static async Task<List<Book>> GetBooksByGenreAsync(Genre genre)
        {
            return await GetBooksByGenreAsync(genre.Id);
        }

        public static async Task<List<Book>> GetBooksByGenreAsync(int genreId)
        {
            using var context = new AppDbContext();
            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(book => book.Genres.Any(g => g.Id == genreId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public static async Task<List<Book>> GetBooksByTagAsync(Tag tag)
        {
            return await GetBooksByTagAsync(tag.Id);
        }

        public static async Task<List<Book>> GetBooksByTagAsync(int tagId)
        {
            using var context = new AppDbContext();
            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(book => book.Tags.Any(t => t.Id == tagId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }
        public static async Task<List<Book>> GetBooksByCollectionAsync(Collection collection)
        {
            return await GetBooksByCollectionAsync(collection.Id);
        }

        public static async Task<List<Book>> GetBooksByCollectionAsync(int collectionId)
        {
            using var context = new AppDbContext();
            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(book => book.Collections.Any(c => c.Id == collectionId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }
        public static async Task<Book?> GetBookByIdAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Books.FirstOrDefaultAsync(book => book.Id == id);
        }

        public static async Task<Book?> GetBookByISBNAsync(string isbn)
        {
            using var context = new AppDbContext();
            return await context.Books.FirstOrDefaultAsync(book => book.ISBN == isbn);
        }

        public static async Task<Book> AddBookWithAuthorsAsync(Book book, List<string> authorNames)
        {
            using var context = new AppDbContext();

            if (book.DateAdded == default)
                book.DateAdded = DateTime.UtcNow;

            // Handle authors
            foreach (var authorName in authorNames)
            {
                var author = await context.Authors.FirstOrDefaultAsync(a => a.Name == authorName);

                if (author == null)
                {
                    author = new Author { Name = authorName };
                    context.Authors.Add(author);
                }

                book.Authors.Add(author);
            }

            context.Books.Add(book);
            await context.SaveChangesAsync();

            return book;
        }

        #endregion

        #region Book Availability
        public static async Task<BookAvailability> GetBookAvailabilityAsync(int bookId)
        {
            using var context = new AppDbContext();

            var book = await context.Books.FindAsync(bookId);
            if (book == null)
                return new BookAvailability { BookExists = false };

            var activeLoans = await context.Loans
                .CountAsync(l => l.BookId == bookId && l.ReturnDate == null);

            return new BookAvailability
            {
                BookExists = true,
                TotalCopies = book.Copies,
                OnLoan = activeLoans,
                Available = book.Copies - activeLoans
            };
        }

        public static async Task<int> GetAvailableCopiesAsync(int bookId)
        {
            using var context = new AppDbContext();

            var book = await context.Books.FindAsync(bookId);
            if (book == null)
                return 0;

            // Count active loans
            var activeLoans = await context.Loans
                .CountAsync(l => l.BookId == bookId && l.ReturnDate == null);

            return book.Copies - activeLoans;
        }

        public static async Task<bool> IsBookAvailableAsync(int bookId)
        {
            return await GetAvailableCopiesAsync(bookId) > 0;
        }
        #endregion

        #region Borrowers

        #endregion

        #region Collections

        #endregion

        #region Genres

        #endregion

        #region Loans
        public static async Task<List<Loan>> GetAllLoansAsync()
        {
            using var context = new AppDbContext();
            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public static async Task<List<Loan>> GetLoansByBookAsync(int bookId)
        {
            using var context = new AppDbContext();

            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .Where(l => l.BookId == bookId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public static async Task<List<Loan>> GetLoansByBorrowerAsync(int borrowerId)
        {
            using var context = new AppDbContext();

            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .Where(l => l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public static async Task<List<Loan>> GetActiveLoansAsync()
        {
            using var context = new AppDbContext();
            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .Where(l => l.ReturnDate == null)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public static async Task<List<Loan>> GetActiveLoansByBorrowerAsync(int borrowerId)
        {
            using var context = new AppDbContext();

            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .Where(l => l.ReturnDate == null && l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public static async Task<List<Loan>> GetOverdueLoansAsync()
        {
            using var context = new AppDbContext();
            var now = DateTime.Now;

            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .Include(l => l.Renewals)
                .Where(l => l.ReturnDate == null)
                .ToListAsync()
                .ContinueWith(task => task.Result
                    .Where(l => l.GetEffectiveDueDate() < now)
                    .OrderBy(l => l.GetEffectiveDueDate())
                    .ToList());
        }

        public static async Task<DateTime> GetCurrentDueDateAsync(int loanId)
        {
            using var context = new AppDbContext();

            var loan = await context.Loans
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.Id == loanId) ?? throw new InvalidOperationException("Loan does not exist.");

            // Use the most recent renewal if there are any
            if (loan.Renewals.Count == 0)
            {
                return loan.Renewals
                    .OrderByDescending(r => r.RenewalDate)
                    .First()
                    .NewDueDate;
            }

            return loan.DueDate;
        }

        public static async Task<Loan> CheckoutBookAsync(int bookId, int borrowerId, int loanPeriodDays = 14)
        {
            using var context = new AppDbContext();

            // Check if book is available
            var book = await context.Books.FindAsync(bookId) ?? throw new InvalidOperationException("Book does not exist.");

            // Check if borrower exists
            var borrower = await context.Borrowers.FindAsync(borrowerId) ?? throw new InvalidOperationException("Borrower does not exist.");

            // Check if borrower is already borrowing this book
            var borrowerHasBook = await context.Loans
                .AnyAsync(l => l.BookId == bookId &&
                                l.BorrowerId == borrowerId &&
                                l.ReturnDate == null);

            if (borrowerHasBook)
                throw new InvalidOperationException("Borrower already has an active loan for this book.");

            // Check availability
            var activeLoans = await context.Loans
                .CountAsync(l => l.BookId == bookId && l.ReturnDate == null);

            if (activeLoans >= book.Copies)
                throw new InvalidOperationException($"No copies available. All {book.Copies} copies are currently on loan.");

            // Create loan
            var loan = new Loan
            {
                BookId = bookId,
                BorrowerId = borrowerId,
                CheckoutDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(loanPeriodDays),
                Renewals = []
            };

            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            return loan;
        }

        public static async Task<Renewal> RenewLoanAsync(int loanId, int loanPeriodDays = 14, int maxRenewals = 3)
        {
            using var context = new AppDbContext();

            // Check if loan exists
            var loan = await context.Loans
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.Id == loanId) ?? throw new InvalidOperationException("Loan does not exist.");

            // Check if loan has already been completed
            if (loan.ReturnDate != null)
                throw new InvalidOperationException("Book has already been returned.");

            // Check if borrower is past max renewals
            if (loan.Renewals.Count >= maxRenewals)
                throw new InvalidOperationException($"Book has already been renewed the max number of times: {maxRenewals}");

            // Create Renewal
            var renewal = new Renewal
            {
                LoanId = loanId,
                RenewalDate = DateTime.Now,
                NewDueDate = DateTime.Now.AddDays(loanPeriodDays)
            };

            context.Renewals.Add(renewal);
            await context.SaveChangesAsync();

            return renewal;
        }

        public static async Task<Loan?> ReturnBookAsync(int loanId)
        {
            using var context = new AppDbContext();

            var loan = await context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
                return null;

            if (loan.ReturnDate != null)
                throw new InvalidOperationException("Book has already been returned.");

            loan.ReturnDate = DateTime.Now;

            await context.SaveChangesAsync();

            return loan;
        }

        #endregion

        #region Renewals

        #endregion

        #region Tags

        #endregion
    }
}
