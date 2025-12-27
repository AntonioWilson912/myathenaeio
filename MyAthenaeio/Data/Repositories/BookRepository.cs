using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Utils;

namespace MyAthenaeio.Data.Repositories
{
    public class BookRepository(AppDbContext context) : Repository<Book>(context), IBookRepository
    {

        #region Query Methods with Include Options

        public async Task<Book?> GetByIdAsync(int id, BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.FirstOrDefaultAsync(b => b.Id == id);
        }

        public override async Task<List<Book>> GetAllAsync()
        {
            return await GetAllAsync(BookIncludeOptions.Default);
        }

        public async Task<List<Book>> GetAllAsync(BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.OrderBy(b => b.Title).ToListAsync();
        }

        public async Task<Book?> GetByISBNAsync(string isbn, BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;

            var (isbn10, isbn13) = ISBNValidator.GetBothISBNFormats(isbn);
            var isbnsToCheck = new List<string>();

            if (!string.IsNullOrEmpty(isbn10))
                isbnsToCheck.Add(isbn10);
            if (!string.IsNullOrEmpty(isbn13))
                isbnsToCheck.Add(isbn13);

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query.FirstOrDefaultAsync(book =>
                isbnsToCheck.Any(searchIsbn => ISBNValidator.CleanISBN(book.ISBN) == searchIsbn));
        }

        public async Task<List<Book>> SearchAsync(string query, BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;
            query = query.ToLower();

            var booksQuery = BuildQuery(_dbSet.AsQueryable(), options);

            return await booksQuery
                .Where(b => b.Title.ToLower().Contains(query) ||
                            (b.Subtitle != null && b.Subtitle.ToLower().Contains(query)) ||
                            (b.Description != null && b.Description.ToLower().Contains(query)) ||
                            b.ISBN.Contains(query) ||
                            (options.IncludeAuthors && b.Authors.Any(a => a.Name.ToLower().Contains(query))))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<List<Book>> GetByAuthorAsync(int authorId, BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(book => book.Authors.Any(a => a.Id == authorId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<List<Book>> GetByGenreAsync(int genreId, BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(book => book.Genres.Any(g => g.Id == genreId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<List<Book>> GetByTagAsync(int tagId, BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(book => book.Tags.Any(t => t.Id == tagId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<List<Book>> GetByCollectionAsync(int collectionId, BookIncludeOptions? options = null)
        {
            options ??= BookIncludeOptions.Default;

            var query = BuildQuery(_dbSet.AsQueryable(), options);

            return await query
                .Where(book => book.Collections.Any(c => c.Id == collectionId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        #endregion

        #region Add Methods

        public override async Task<Book> AddAsync(Book book)
        {
            return await AddAsync(book, []);
        }

        public async Task<Book> AddAsync(
            Book book,
            List<AuthorInfo> authorInfos,
            List<int>? genreIds = null,
            List<int>? tagIds = null,
            List<int>? collectionIds = null)
        {
            try
            {
                // Check for existing book using both ISBN formats
                var (isbn10, isbn13) = ISBNValidator.GetBothISBNFormats(book.ISBN);

                var isbnsToCheck = new List<string>();
                if (!string.IsNullOrEmpty(isbn10))
                    isbnsToCheck.Add(isbn10);
                if (!string.IsNullOrEmpty(isbn13))
                    isbnsToCheck.Add(isbn13);

                var existingBook = await _context.Books
                    .FirstOrDefaultAsync(b => isbnsToCheck.Any(searchIsbn =>
                        ISBNValidator.CleanISBN(b.ISBN) == searchIsbn));

                if (existingBook != null)
                    throw new InvalidOperationException(
                        $"Book already exists: {existingBook.Title} (ID: {existingBook.Id})");

                // Set defaults
                if (book.DateAdded == default)
                    book.DateAdded = DateTime.UtcNow;

                // Handle authors with OpenLibrary keys
                foreach (var authorInfo in authorInfos)
                {
                    Author? author = null;

                    // First try to find by OpenLibrary key (most reliable)
                    if (!string.IsNullOrEmpty(authorInfo.OpenLibraryKey))
                    {
                        author = await _context.Authors
                            .FirstOrDefaultAsync(a => a.OpenLibraryKey == authorInfo.OpenLibraryKey);
                    }

                    // If not found by key, try by name (for backwards compatibility)
                    if (author == null && !string.IsNullOrEmpty(authorInfo.Name))
                    {
                        author = await _context.Authors
                            .FirstOrDefaultAsync(a => a.Name == authorInfo.Name && a.OpenLibraryKey == null);
                    }

                    // Create new author if not found
                    if (author == null)
                    {
                        author = new Author
                        {
                            Name = authorInfo.Name,
                            OpenLibraryKey = authorInfo.OpenLibraryKey,
                            Bio = authorInfo.Bio
                        };
                        _context.Authors.Add(author);
                        System.Diagnostics.Debug.WriteLine(
                            $"Creating new author: {author.Name} (Key: {author.OpenLibraryKey})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Found existing author: {author.Name} (ID: {author.Id}, Key: {author.OpenLibraryKey})");

                        // Update bio if we have new info and didn't have it before
                        if (string.IsNullOrEmpty(author.Bio) && !string.IsNullOrEmpty(authorInfo.Bio))
                        {
                            author.Bio = authorInfo.Bio;
                        }
                    }

                    book.Authors.Add(author);
                }

                // Handle genres
                if (genreIds != null && genreIds.Count != 0)
                {
                    var genres = await _context.Genres
                        .Where(g => genreIds.Contains(g.Id))
                        .ToListAsync();

                    foreach (var genre in genres)
                        book.Genres.Add(genre);
                }

                // Handle tags
                if (tagIds != null && tagIds.Count != 0)
                {
                    var tags = await _context.Tags
                        .Where(t => tagIds.Contains(t.Id))
                        .ToListAsync();

                    foreach (var tag in tags)
                        book.Tags.Add(tag);
                }

                // Handle collections
                if (collectionIds != null && collectionIds.Count != 0)
                {
                    var collections = await _context.Collections
                        .Where(c => collectionIds.Contains(c.Id))
                        .ToListAsync();

                    foreach (var collection in collections)
                        book.Collections.Add(collection);
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                var firstCopy = new BookCopy
                {
                    BookId = book.Id,
                    CopyNumber = "Copy 1",
                    AcquisitionDate = DateTime.Now,
                    IsAvailable = true
                };
                _context.BookCopies.Add(firstCopy);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Book saved successfully with ID: {book.Id}");
                return book;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"INNER: {ex.InnerException?.Message}");
                throw;
            }
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Book book)
        {
            var existingBook = await _context.Books
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

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAuthorsAsync(int bookId, List<int> authorIds)
        {
            var book = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Authors.Clear();

            var authors = await _context.Authors
                .Where(a => authorIds.Contains(a.Id))
                .ToListAsync();

            foreach (var author in authors)
            {
                book.Authors.Add(author);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateGenresAsync(int bookId, List<int> genreIds)
        {
            var book = await _context.Books
                .Include(b => b.Genres)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Genres.Clear();

            var genres = await _context.Genres
                .Where(g => genreIds.Contains(g.Id))
                .ToListAsync();

            foreach (var genre in genres)
            {
                book.Genres.Add(genre);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateTagsAsync(int bookId, List<int> tagIds)
        {
            var book = await _context.Books
                .Include(b => b.Tags)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Tags.Clear();

            var tags = await _context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();

            foreach (var tag in tags)
            {
                book.Tags.Add(tag);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateCollectionsAsync(int bookId, List<int> collectionIds)
        {
            var book = await _context.Books
                .Include(b => b.Collections)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                throw new InvalidOperationException("Book does not exist.");

            book.Collections.Clear();

            var collections = await _context.Collections
                .Where(c => collectionIds.Contains(c.Id))
                .ToListAsync();

            foreach (var collection in collections)
            {
                book.Collections.Add(collection);
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region

        public override async Task DeleteAsync(int bookId)
        {
            var book = await GetByIdAsync(bookId) ?? throw new InvalidOperationException("Book does not exist.");

            // Check for active loans
            var hasActiveLoans = await _context.Loans
                .AnyAsync(l => l.BookId == bookId && l.ReturnDate == null);

            if (hasActiveLoans)
                throw new InvalidOperationException($"Cannot delete book '{book.Title}' because it is currently on loan.");

            await base.DeleteAsync(book);
        }

        #endregion

        #region Availability Methods

        public async Task<BookAvailability> GetAvailabilityAsync(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Copies)
                    .ThenInclude(bc => bc.Loans.Where(l => l.ReturnDate == null))
                        .ThenInclude(l => l.Borrower)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                return new BookAvailability { BookExists = false };

            var copyStatuses = book.Copies.Select(copy =>
            {
                var currentLoan = copy.Loans.FirstOrDefault(l => l.ReturnDate == null);
                return new BookCopyStatus
                {
                    CopyId = copy.Id,
                    CopyNumber = copy.CopyNumber,
                    IsAvailable = copy.IsAvailable,
                    BorrowerName = currentLoan?.Borrower?.Name,
                    DueDate = currentLoan?.DueDate
                };
            }).ToList();

            return new BookAvailability
            {
                BookExists = true,
                TotalCopies = book.Copies.Count,
                AvailableCopies = book.Copies.Count(c => c.IsAvailable),
                CopyStatuses = copyStatuses
            };
        }

        public async Task<int> GetAvailableCopiesAsync(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
                return 0;

            var activeLoans = await _context.Loans
                .CountAsync(l => l.BookId == bookId && l.ReturnDate == null);

            return book.Copies.Count - activeLoans;
        }

        public async Task<bool> IsAvailableAsync(int bookId)
        {
            var availableCopies = await GetAvailableCopiesAsync(bookId);
            return availableCopies > 0;
        }

        #endregion

        #region Helper Methods

        private static IQueryable<Book> BuildQuery(IQueryable<Book> query, BookIncludeOptions options)
        {
            if (options.IncludeAuthors)
                query = query.Include(b => b.Authors);

            if (options.IncludeGenres)
                query = query.Include(b => b.Genres);

            if (options.IncludeTags)
                query = query.Include(b => b.Tags);

            if (options.IncludeCollections)
                query = query.Include(b => b.Collections);

            return query;
        }

        #endregion
    }
}