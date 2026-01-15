using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Data.Repositories
{
    public class AuthorRepository(AppDbContext context) : Repository<Author>(context), IAuthorRepository
    {

        #region Query Methods with Include Options

        public async Task<Author?> GetByIdAsync(int id, AuthorIncludeOptions? options = null)
        {
            options ??= AuthorIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.FirstOrDefaultAsync(a => a.Id == id);
        }

        public override async Task<List<Author>> GetAllAsync()
        {
            return await GetAllAsync(AuthorIncludeOptions.Default);
        }

        public async Task<List<Author>> GetAllAsync(AuthorIncludeOptions? options = null)
        {
            options ??= AuthorIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable(), options);
            return await query.OrderBy(a => a.Name).ToListAsync();
        }

        public override async Task<List<Author>> GetAllAsNoTrackingAsync()
        {
            return await GetAllAsNoTrackingAsync(AuthorIncludeOptions.Default);
        }

        public async Task<List<Author>> GetAllAsNoTrackingAsync(AuthorIncludeOptions? options = null)
        {
            options ??= AuthorIncludeOptions.Default;

            if (options.ForceReload)
                DetachAll();

            var query = BuildQuery(_dbSet.AsQueryable().AsNoTracking(), options);
            return await query.OrderBy(a => a.Name).ToListAsync();
        }

        public async Task<List<Author>> SearchAsync(string query, AuthorIncludeOptions? options = null)
        {
            options ??= AuthorIncludeOptions.Default;
            query = query.ToLower();

            var authorsQuery = BuildQuery(_dbSet.AsQueryable(), options);

            return await authorsQuery
                .Where(a => a.Name.ToLower().Contains(query) ||
                            (a.Bio != null && a.Bio.ToLower().Contains(query)))
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<Author?> GetByNameAsync(string name)
        {
            return await FirstOrDefaultAsync(a => a.Name == name);
        }

        public async Task<Author?> GetByOpenLibraryKeyAsync(string openLibraryKey)
        {
            if (string.IsNullOrEmpty(openLibraryKey))
                return null;

            return await FirstOrDefaultAsync(a => a.OpenLibraryKey == openLibraryKey);
        }

        #endregion

        #region Author-Specific Operations

        public async Task<List<Book>> GetBooksByAuthorAsync(int authorId)
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(b => b.Authors.Any(a => a.Id == authorId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<List<Author>> GetByBookAsync(int bookId, AuthorIncludeOptions? options = null)
        {
            options ??= AuthorIncludeOptions.Default;

            var authorsQuery = BuildQuery(_dbSet.AsQueryable(), options);

            return await authorsQuery
                .Where(author => author.Books.Any(b => b.Id == bookId))
                .ToListAsync();
        }

        #endregion

        #region Add Methods with Validation

        public override async Task<Author> AddAsync(Author author)
        {
            // Validation
            ValidateAuthor(author);

            return await base.AddAsync(author);
        }

        private static void ValidateAuthor(Author author)
        {
            ArgumentNullException.ThrowIfNull(author);

            if (string.IsNullOrWhiteSpace(author.Name))
                throw new ArgumentException("Author name is required.", nameof(author));

            if (author.Name.Length > 200)
                throw new ArgumentException("Author name cannot exceed 200 characters.", nameof(author));
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Author author)
        {
            // Validation
            ValidateAuthor(author);

            var existingAuthor = await _context.Authors.FindAsync(author.Id) ?? throw new InvalidOperationException("Author does not exist.");

            existingAuthor.Name = author.Name;
            existingAuthor.Bio = author.Bio;
            existingAuthor.OpenLibraryKey = author.OpenLibraryKey;
            existingAuthor.BirthDate = author.BirthDate;
            existingAuthor.PhotoUrl = author.PhotoUrl;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            var author = await GetByIdAsync(id) ?? throw new InvalidOperationException("Author does not exist.");

            // Prevent deletion if author has books
            var hasBooks = await _context.Books
                .AnyAsync(b => b.Authors.Any(a => a.Id == id));

            if (hasBooks)
                throw new InvalidOperationException(
                    $"Cannot delete author '{author.Name}' because they have books in the library. " +
                    "Remove this author from all books first, or delete the books.");

            await base.DeleteAsync(author);
        }

        #endregion

        #region Helper Methods

        private static IQueryable<Author> BuildQuery(IQueryable<Author> query, AuthorIncludeOptions options)
        {
            if (options.IncludeBooks)
            {
                query = query.Include(a => a.Books);
            }

            return query;
        }

        #endregion
    }
}