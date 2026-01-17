using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using Serilog;

namespace MyAthenaeio.Data.Repositories
{
    public class GenreRepository(AppDbContext context) : Repository<Genre>(context), IGenreRepository
    {
        private static readonly ILogger _logger = Log.ForContext<GenreRepository>();

        #region Query Methods

        public async Task<List<Genre>> SearchAsync(string query)
        {
            query = query.ToLower();

            return await _dbSet
                .Where(g => g.Name.ToLower().Contains(query))
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<Genre?> GetByNameAsync(string name)
        {
            return await FirstOrDefaultAsync(g => g.Name == name);
        }

        #endregion

        #region Book-Related Queries

        public async Task<List<Book>> GetBooksByGenreAsync(int genreId)
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(b => b.Genres.Any(g => g.Id == genreId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<int> GetBookCountAsync(int genreId)
        {
            return await _context.Books
                .CountAsync(b => b.Genres.Any(g => g.Id == genreId));
        }

        public async Task<bool> HasBooksAsync(int genreId)
        {
            return await _context.Books
                .AnyAsync(b => b.Genres.Any(g => g.Id == genreId));
        }

        #endregion

        #region Add Methods with Validation

        public override async Task<Genre> AddAsync(Genre genre)
        {
            try
            {
                ValidateGenre(genre);

                _logger.Debug("Adding genre: {Name}", genre.Name);

                var existingGenre = await FirstOrDefaultAsync(g =>
                    g.Name.ToLower() == genre.Name.ToLower());

                if (existingGenre != null)
                {
                    _logger.Warning("Cannot add genre - duplicate name: {Name}", genre.Name);
                    throw new InvalidOperationException(
                        $"Genre '{genre.Name}' already exists.");
                }

                var result = await base.AddAsync(genre);

                _logger.Information("Genre added: {Name} (ID: {Id})", genre.Name, genre.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add genre: {Name}", genre.Name);
                throw;
            }
        }

        private static void ValidateGenre(Genre genre)
        {
            ArgumentNullException.ThrowIfNull(genre);

            if (string.IsNullOrWhiteSpace(genre.Name))
                throw new ArgumentException("Genre name is required.", nameof(genre));

            if (genre.Name.Length > 100)
                throw new ArgumentException("Genre name cannot exceed 100 characters.", nameof(genre));
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Genre genre)
        {
            try
            {
                ValidateGenre(genre);

                var existingGenre = await _context.Genres.FindAsync(genre.Id);

                if (existingGenre == null)
                {
                    _logger.Warning("Update failed: Genre {Id} not found", genre.Id);
                    throw new InvalidOperationException("Genre does not exist.");
                }

                _logger.Debug("Updating genre: {Name} (ID: {Id})", genre.Name, genre.Id);

                var duplicateGenre = await FirstOrDefaultAsync(g =>
                    g.Id != genre.Id &&
                    g.Name.ToLower() == genre.Name.ToLower());

                if (duplicateGenre != null)
                {
                    _logger.Warning("Update failed: duplicate name {Name} for genre {Id}",
                        genre.Name, genre.Id);
                    throw new InvalidOperationException(
                        $"Genre '{genre.Name}' already exists.");
                }

                existingGenre.Name = genre.Name;

                await _context.SaveChangesAsync();

                _logger.Information("Genre updated: {Name} (ID: {Id})", genre.Name, genre.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update genre ID: {Id}", genre.Id);
                throw;
            }
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            try
            {
                var genre = await GetByIdAsync(id);
                if (genre == null)
                {
                    _logger.Warning("Delete failed: Genre {Id} not found", id);
                    throw new InvalidOperationException("Genre does not exist.");
                }

                await base.DeleteAsync(genre);
                _logger.Information("Genre deleted: {Name} (ID: {Id})", genre.Name, id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete genre ID: {Id}", id);
                throw;
            }
        }

        #endregion

        #region Override GetAllAsync

        public override async Task<List<Genre>> GetAllAsync()
        {
            return await _dbSet.OrderBy(g => g.Name).ToListAsync();
        }

        #endregion
    }
}