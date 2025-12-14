using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Data;
using MyAthenaeio.Models;
using MyAthenaeio.Utils;

namespace MyAthenaeio.Services
{
    public static class LibraryService
    {
        #region Authors

        public static async Task<Author> CreateAuthorAsync(string name, string? bio = null)
        {
            using var context = new AppDbContext();

            Author author = new()
            {
                Name = name,
                Bio = bio
            };

            await context.Authors.AddAsync(author);
            await context.SaveChangesAsync();

            return author;
        }

        public static async Task<List<Author>> GetAuthorsAsync()
        {
            using var context = new AppDbContext();

            return await context.Authors
                .Include(a => a.Books)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public static async Task<Author?> GetAuthorAsync(int id)
        {
            using var context = new AppDbContext();

            return await context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(author => author.Id == id);
        }

        public static async Task<List<Author>> SearchAuthorsAsync(string query)
        {
            using var context = new AppDbContext();

            return await context.Authors
                .Include(a => a.Books)
                .Where(a => a.Name.ToLower().Contains(query.ToLower()))
                .ToListAsync();
        }

        public static async Task<Author?> UpdateAuthorAsync(Author author)
        {
            using var context = new AppDbContext();

            var existingAuthor = await context.Authors.FindAsync(author.Id) ?? throw new InvalidOperationException("Author does not exist.");
            existingAuthor.Name = author.Name;
            existingAuthor.Bio = author.Bio;

            await context.SaveChangesAsync();

            return existingAuthor;
        }

        #endregion

        #region Books

        public static async Task<Book> AddBookAsync(Book book, List<string> authorNames, List<int>? genreIds = null, List<int>? tagIds = null, List<int>? collectionIds = null)
        {
            using var context = new AppDbContext();

            var existingBook = await context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.ISBN == book.ISBN);

            if (existingBook != null)
                throw new InvalidOperationException($"Book already exists: {existingBook.Title} (ID: {existingBook.Id})");

            if (book.DateAdded == default)
                book.DateAdded = DateTime.UtcNow;

            if (book.Copies == 0)
                book.Copies = 1;

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

            // Handle genres
            if (genreIds != null && genreIds.Count != 0)
            {
                var genres = await context.Genres
                    .Where(g => genreIds.Contains(g.Id))
                    .ToListAsync();

                foreach (var genre in genres)
                    book.Genres.Add(genre);
            }

            // Handle tags
            if (tagIds != null && tagIds.Count != 0)
            {
                var tags = await context.Tags
                    .Where(t => tagIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tag in tags)
                    book.Tags.Add(tag);
            }

            // Handle collections
            if (collectionIds != null && collectionIds.Count != 0)
            {
                var collections = await context.Collections
                    .Where(c => collectionIds.Contains(c.Id))
                    .ToListAsync();

                foreach (var collection in collections)
                    book.Collections.Add(collection);
            }

            context.Books.Add(book);
            await context.SaveChangesAsync();

            return book;
        }

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
            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .FirstOrDefaultAsync(book => book.Id == id);
        }

        public static async Task<Book?> GetBookByISBNAsync(string isbn)
        {
            using var context = new AppDbContext();

            // Clean ISBN
            isbn = isbn.Replace("-", "").Replace(" ", "");

            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .FirstOrDefaultAsync(book => book.ISBN.Replace("-", "").Replace(" ", "") == isbn);
        }

        public static async Task<List<Book>> SearchBooksAsync(string query)
        {
            using var context = new AppDbContext();

            query = query.ToLower();
            return await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(b => b.Title.ToLower().Contains(query) ||
                            b.Subtitle!.ToLower().Contains(query) ||
                            b.Description!.ToLower().Contains(query) ||
                            b.ISBN.Contains(query) ||
                            b.Authors.Any(a => a.Name.ToLower().Contains(query)))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public static async Task<Book> UpdateBookAsync(Book book)
        {
            using var context = new AppDbContext();

            var existingBook = await context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .FirstOrDefaultAsync(b => b.Id == book.Id);
                
            if (existingBook == null)
                throw new InvalidOperationException("Book does not exist.");

            existingBook.Title = book.Title;
            existingBook.Subtitle = book.Subtitle;
            existingBook.Description = book.Description;
            existingBook.Publisher = book.Publisher;
            existingBook.PublicationYear = book.PublicationYear;
            existingBook.CoverImageUrl = book.CoverImageUrl;
            existingBook.Copies = book.Copies;
            existingBook.Notes = book.Notes;

            await context.SaveChangesAsync();
            return existingBook;
        }

        public static async Task UpdateBookAuthorsAsync(int bookId, List<int> authorIds)
        {
            using var context = new AppDbContext();

            var book = await context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Authors.Clear();

            var authors = await context.Authors
                .Where(a => authorIds.Contains(a.Id))
                .ToListAsync();

            foreach (var author in authors)
            {
                book.Authors.Add(author);
            }

            await context.SaveChangesAsync();
        }

        public static async Task UpdateBookGenresAsync(int bookId, List<int> genreIds)
        {
            using var context = new AppDbContext();

            var book = await context.Books
                .Include(b => b.Genres)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Genres.Clear();

            var genres = await context.Genres
                .Where(g => genreIds.Contains(g.Id))
                .ToListAsync();

            foreach (var genre in genres)
            {
                book.Genres.Add(genre);
            }

            await context.SaveChangesAsync();
        }

        public static async Task UpdateBookTagsAsync(int bookId, List<int> tagIds)
        {
            using var context = new AppDbContext();

            var book = await context.Books
                .Include(b => b.Tags)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Tags.Clear();

            var tags = await context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();

            foreach (var tag in tags)
            {
                book.Tags.Add(tag);
            }

            await context.SaveChangesAsync();
        }

        public static async Task UpdateBookCollectionsAsync(int bookId, List<int> collectionIds)
        {
            using var context = new AppDbContext();

            var book = await context.Books
                .Include(b => b.Collections)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Collections.Clear();

            var collections = await context.Collections
                .Where(c => collectionIds.Contains(c.Id))
                .ToListAsync();

            foreach (var collection in collections)
            {
                book.Collections.Add(collection);
            }

            await context.SaveChangesAsync();
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

        public static async Task<Borrower> CreateBorrowerAsync(string name, string? email = null, string? phone = null)
        {
            using var context = new AppDbContext();

            // Check for duplicate email
            if (!string.IsNullOrEmpty(email))
            {
                var emailExists = await context.Borrowers
                    .AnyAsync(b => b.Email != null && b.Email.ToLower() == email.ToLower());

                if (emailExists)
                    throw new InvalidOperationException($"A borrower with email '{email}' already exists.");
            }

            // Check for duplicate phone
            if (!string.IsNullOrEmpty(phone))
            {
                var phoneExists = await context.Borrowers
                    .AnyAsync(b => b.Phone != null && b.Phone == phone);

                if (phoneExists)
                    throw new InvalidOperationException($"A borrower with phone '{phone}' already exists.");
            }

            Borrower borrower = new()
            {
                Name = name,
                Email = email,
                Phone = phone,
                DateAdded = DateTime.Now
            };

            context.Add(borrower);
            await context.SaveChangesAsync();

            return borrower;
        }

        public static async Task<List<Borrower>> GetBorrowersAsync()
        {
            using var context = new AppDbContext();

            return await context.Borrowers
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public static async Task<List<Borrower>> SearchBorrowersAsync(string query)
        {
            using var context = new AppDbContext();

            query = query.ToLower();
            return await context.Borrowers
                .Where(b => b.Name.ToLower().Contains(query) ||
                            b.Email!.ToLower().Contains(query) ||
                            b.Phone!.Contains(query))
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public static async Task<Borrower?> GetBorrowerAsync(int borrowerId)
        {
            using var context = new AppDbContext();

            return await context.Borrowers
                .FirstOrDefaultAsync(b => b.Id == borrowerId);
        }

        public static async Task<Borrower> UpdateBorrowerAsync(Borrower borrower)
        {
            using var context = new AppDbContext();

            var existingBorrower = await context.Borrowers.FirstOrDefaultAsync(b => b.Id == borrower.Id);

            if (existingBorrower == null)
                throw new InvalidOperationException("Borrower does not exist.");

            if (!string.IsNullOrEmpty(borrower.Email))
            {
                var existingEmail = await context.Borrowers.AnyAsync(b => b.Id != existingBorrower.Id && b.Email!.ToLower() == borrower.Email.ToLower());

                if (existingEmail)
                    throw new InvalidOperationException($"Borrower already exists with email '{borrower.Email}'");

                existingBorrower.Email = borrower.Email;
            }

            if (!string.IsNullOrEmpty(borrower.Phone))
            {
                var existingPhone = await context.Borrowers.AnyAsync(b => b.Id != existingBorrower.Id && b.Phone!.ToLower() == borrower.Phone.ToLower());

                if (existingPhone)
                    throw new InvalidOperationException($"Borrower already exists with phone '{borrower.Phone}'");

                existingBorrower.Phone = borrower.Phone;
            }

            existingBorrower.Name = borrower.Name;

            await context.SaveChangesAsync();

            return existingBorrower;
        }

        #endregion

        #region Collections
        public static async Task<Collection> CreateCollectionAsync(string name, string notes = "")
        {
            using var context = new AppDbContext();

            var existingCollection = await context.Collections.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

            if (existingCollection != null)
                throw new InvalidOperationException($"Collection '{name}' already exists.");

            Collection collection = new()
            {
                Name = name,
                Notes = notes
            };

            await context.Collections.AddAsync(collection);
            await context.SaveChangesAsync();

            return collection;
        }

        public static async Task<List<Collection>> GetCollectionsAsync()
        {
            using var context = new AppDbContext();

            return await context.Collections
                .Include(c => c.Books)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public static async Task<List<Collection>> SearchCollectionsAsync(string query)
        {
            using var context = new AppDbContext();

            query = query.ToLower();

            return await context.Collections
                .Include(c => c.Books)
                .Where(c => c.Name.ToLower() == query)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public static async Task UpdateCollectionAsync(Collection collection)
        {
            using var context = new AppDbContext();

            var existingCollection = await context.Collections.FindAsync(collection.Id) ?? throw new InvalidOperationException("Collection does not exist.");

            var duplicateCollection = await context.Collections
                .FirstOrDefaultAsync(c => c.Id != collection.Id && c.Name.ToLower() == collection.Name.ToLower());

            if (duplicateCollection != null)
                throw new InvalidOperationException($"Collection '{collection.Name} already exists.");

            existingCollection.Name = collection.Name;
            await context.SaveChangesAsync();
        }

        public static async Task DeleteCollectionAsync(int collectionId)
        {
            using var context = new AppDbContext();

            var existingCollection = await context.Collections.FindAsync(collectionId) ?? throw new InvalidOperationException("Collection does not exist.");

            context.Collections.Remove(existingCollection);
            await context.SaveChangesAsync();
        }

        #endregion

        #region Genres

        public static async Task<Genre> CreateGenreAsync(string name)
        {
            using var context = new AppDbContext();

            var existingGenre = await context.Genres.FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

            if (existingGenre != null)
                throw new InvalidOperationException($"Genre '{name}' already exists.");

            Genre genre = new()
            {
                Name = name
            };

            await context.Genres.AddAsync(genre);
            await context.SaveChangesAsync();

            return genre;
        }

        public static async Task<List<Genre>> GetGenresAsync()
        {
            using var context = new AppDbContext();

            return await context.Genres
                .Include(g => g.Books)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public static async Task<List<Genre>> SearchGenresAsync(string query)
        {
            using var context = new AppDbContext();

            query = query.ToLower();

            return await context.Genres
                .Include(c => c.Books)
                .Where(g => g.Name.ToLower() == query)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public static async Task UpdateGenreAsync(Genre genre)
        {
            using var context = new AppDbContext();

            var existingGenre = await context.Genres.FindAsync(genre.Id) ?? throw new InvalidOperationException("Genre does not exist.");

            var duplicateGenre = await context.Genres
                .FirstOrDefaultAsync(t => t.Id != genre.Id && t.Name.ToLower() == genre.Name.ToLower());

            if (duplicateGenre != null)
                throw new InvalidOperationException($"Genre '{genre.Name}' already exists.");

            existingGenre.Name = genre.Name;
            await context.SaveChangesAsync();
        }

        public static async Task DeleteGenreAsync(int genreId)
        {
            using var context = new AppDbContext();

            var existingGenre = await context.Genres.FindAsync(genreId) ?? throw new InvalidOperationException("Genre does not exist.");

            context.Genres.Remove(existingGenre);
            await context.SaveChangesAsync();
        }

        #endregion

        #region Loans
        public static async Task<List<Loan>> GetAllLoansAsync()
        {
            using var context = new AppDbContext();
            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .Include(l => l.Renewals)
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
                .Include(l => l.Renewals)
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
                .Include(l => l.Renewals)
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
                .Include(l => l.Renewals)
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
                .Include(l => l.Renewals)
                .Where(l => l.ReturnDate == null && l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.CheckoutDate)
                .ToListAsync();
        }

        public static async Task<List<Loan>> GetLoansDueSoonAsync(int daysAhead = 7)
        {
            using var context = new AppDbContext();
            var now = DateTime.Now;
            var futureDate = now.AddDays(daysAhead);

            return await context.Loans
                .Include(l => l.Book)
                    .ThenInclude(b => b.Authors)
                .Include(l => l.Borrower)
                .Include(l => l.Renewals)
                .Where(l => l.ReturnDate == null)
                .ToListAsync()
                .ContinueWith(task => task.Result
                    .Where(l => l.GetEffectiveDueDate() >= now &&
                                l.GetEffectiveDueDate() <= futureDate)
                    .OrderBy(l => l.GetEffectiveDueDate())
                    .ToList());
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
            if (loan.Renewals.Count > 0)
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

        public static async Task<Loan> ReturnBookAsync(int loanId)
        {
            using var context = new AppDbContext();

            var loan = await context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
                throw new InvalidOperationException("Loan does not exist.");

            if (loan.ReturnDate != null)
                throw new InvalidOperationException("Book has already been returned.");

            loan.ReturnDate = DateTime.Now;

            await context.SaveChangesAsync();

            return loan;
        }

        public static async Task<Loan> UpdateLoanAsync(Loan loan)
        {
            using var context = new AppDbContext();

            var existingLoan = await context.Loans
                .Include(l => l.Book)
                .Include(l => l.Renewals)
                .FirstOrDefaultAsync(l => l.Id == loan.Id);

            if (existingLoan == null)
                throw new InvalidOperationException("Loan does not exist.");

            existingLoan.CheckoutDate = loan.CheckoutDate;
            existingLoan.DueDate = loan.DueDate;
            existingLoan.ReturnDate = loan.ReturnDate;
            existingLoan.Notes = loan.Notes;

            await context.SaveChangesAsync();

            return existingLoan;
        }

        #endregion

        #region Renewals

        public static async Task<Renewal> UpdateRenewalAsync(Renewal renewal)
        {
            using var context = new AppDbContext();

            var existingRenewal = await context.Renewals
                .Include(r => r.Loan)
                .FirstOrDefaultAsync(r => r.Id == renewal.Id);

            if (existingRenewal == null)
                throw new InvalidOperationException("Renewal does not exist.");

            existingRenewal.NewDueDate = renewal.NewDueDate;
            existingRenewal.Notes = renewal.Notes;

            await context.SaveChangesAsync();

            return existingRenewal;
        }


        #endregion

        #region Tags

        public static async Task<Tag> CreateTagAsync(string name, string notes = "")
        {
            using var context = new AppDbContext();

            var existingTag = await context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

            if (existingTag != null)
                throw new InvalidOperationException($"Tag '{name}' already exists.");

            Tag tag = new()
            {
                Name = name
            };

            await context.Tags.AddAsync(tag);
            await context.SaveChangesAsync();

            return tag;
        }

        public static async Task<List<Tag>> GetTagsAsync()
        {
            using var context = new AppDbContext();

            return await context.Tags
                .Include(t => t.Books)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public static async Task<List<Tag>> SearchTagsAsync(string query)
        {
            using var context = new AppDbContext();

            query = query.ToLower();

            return await context.Tags
                .Include(t => t.Books)
                .Where(t => t.Name.ToLower() == query)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public static async Task UpdateTagAsync(Tag tag)
        {
            using var context = new AppDbContext();

            var existingTag = await context.Tags.FindAsync(tag.Id) ?? throw new InvalidOperationException("Tag does not exist.");

            var duplicateTag = await context.Tags
                .FirstOrDefaultAsync(t => t.Id != tag.Id && t.Name.ToLower() == tag.Name.ToLower());

            if (duplicateTag != null)
                throw new InvalidOperationException($"Tag '{tag.Name}' already exists.");

            existingTag.Name = tag.Name;
            await context.SaveChangesAsync();
        }

        public static async Task DeleteTagAsync(int tagId)
        {
            using var context = new AppDbContext();

            var existingTag = await context.Tags.FindAsync(tagId) ?? throw new InvalidOperationException("Tag does not exist.");

            context.Tags.Remove(existingTag);
            await context.SaveChangesAsync();
        }

        #endregion
    }
}
