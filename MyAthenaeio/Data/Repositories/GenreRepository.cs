using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public class GenreRepository(AppDbContext context) : Repository<Genre>(context), IGenreRepository
    {

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
            // Validation
            ValidateGenre(genre);

            // Check for duplicate name
            var existingGenre = await FirstOrDefaultAsync(g =>
                g.Name.ToLower() == genre.Name.ToLower());

            if (existingGenre != null)
                throw new InvalidOperationException(
                    $"Genre '{genre.Name}' already exists.");

            return await base.AddAsync(genre);
        }

        private static void ValidateGenre(Genre genre)
        {
            if (genre == null)
                throw new ArgumentNullException(nameof(genre));

            if (string.IsNullOrWhiteSpace(genre.Name))
                throw new ArgumentException("Genre name is required.", nameof(genre));

            if (genre.Name.Length > 100)
                throw new ArgumentException("Genre name cannot exceed 100 characters.", nameof(genre));
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Genre genre)
        {
            // Validation
            ValidateGenre(genre);

            var existingGenre = await _context.Genres.FindAsync(genre.Id);

            if (existingGenre == null)
                throw new InvalidOperationException("Genre does not exist.");

            // Check for duplicate name (excluding current genre)
            var duplicateGenre = await FirstOrDefaultAsync(g =>
                g.Id != genre.Id &&
                g.Name.ToLower() == genre.Name.ToLower());

            if (duplicateGenre != null)
                throw new InvalidOperationException(
                    $"Genre '{genre.Name}' already exists.");

            existingGenre.Name = genre.Name;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            var genre = await GetByIdAsync(id) ?? throw new InvalidOperationException("Genre does not exist.");

            // Delete the genre
            await base.DeleteAsync(genre);
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