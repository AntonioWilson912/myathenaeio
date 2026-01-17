using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using Serilog;

namespace MyAthenaeio.Data.Repositories
{
    public class AuthorRepository(AppDbContext context) : Repository<Author>(context), IAuthorRepository
    {
        private static readonly ILogger _logger = Log.ForContext<AuthorRepository>();

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
            try
            {
                ValidateAuthor(author);

                _logger.Debug("Adding author: {Name}", author.Name);

                var result = await base.AddAsync(author);

                _logger.Information("Author added: {Name} (ID: {Id})", author.Name, author.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add author: {Name}", author.Name);
                throw;
            }
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
            try
            {
                ValidateAuthor(author);

                var existingAuthor = await _context.Authors.FindAsync(author.Id);
                if (existingAuthor == null)
                {
                    _logger.Warning("Update failed: Author {Id} not found", author.Id);
                    throw new InvalidOperationException("Author does not exist.");
                }

                _logger.Debug("Updating author: {Name} (ID: {Id})", author.Name, author.Id);

                existingAuthor.Name = author.Name;
                existingAuthor.Bio = author.Bio;
                existingAuthor.OpenLibraryKey = author.OpenLibraryKey;
                existingAuthor.BirthDate = author.BirthDate;
                existingAuthor.PhotoUrl = author.PhotoUrl;

                await _context.SaveChangesAsync();

                _logger.Information("Author updated: {Name} (ID: {Id})", author.Name, author.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update author ID: {Id}", author.Id);
                throw;
            }
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            try
            {
                var author = await GetByIdAsync(id);
                if (author == null)
                {
                    _logger.Warning("Delete failed: Author {Id} not found", id);
                    throw new InvalidOperationException("Author does not exist.");
                }

                var hasBooks = await _context.Books
                    .AnyAsync(b => b.Authors.Any(a => a.Id == id));

                if (hasBooks)
                {
                    _logger.Warning("Cannot delete author {Id} '{Name}' - has books", id, author.Name);
                    throw new InvalidOperationException(
                        $"Cannot delete author '{author.Name}' because they have books in the library. " +
                        "Remove this author from all books first, or delete the books.");
                }

                await base.DeleteAsync(author);
                _logger.Information("Author deleted: {Name} (ID: {Id})", author.Name, id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete author ID: {Id}", id);
                throw;
            }
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