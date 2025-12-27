using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public class TagRepository(AppDbContext context) : Repository<Tag>(context), ITagRepository
    {

        #region Query Methods

        public async Task<List<Tag>> SearchAsync(string query)
        {
            query = query.ToLower();

            return await _dbSet
                .Where(t => t.Name.ToLower().Contains(query))
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tag?> GetByNameAsync(string name)
        {
            return await FirstOrDefaultAsync(t => t.Name == name);
        }

        #endregion

        #region Book-Related Queries

        public async Task<List<Book>> GetBooksByTagAsync(int tagId)
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(b => b.Tags.Any(t => t.Id == tagId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<int> GetBookCountAsync(int tagId)
        {
            return await _context.Books
                .CountAsync(b => b.Tags.Any(t => t.Id == tagId));
        }

        public async Task<bool> HasBooksAsync(int tagId)
        {
            return await _context.Books
                .AnyAsync(b => b.Tags.Any(t => t.Id == tagId));
        }

        #endregion

        #region Add Methods with Validation

        public override async Task<Tag> AddAsync(Tag tag)
        {
            // Validation
            ValidateTag(tag);

            // Check for duplicate name
            var existingTag = await FirstOrDefaultAsync(t =>
                t.Name.ToLower() == tag.Name.ToLower());

            if (existingTag != null)
                throw new InvalidOperationException(
                    $"Tag '{tag.Name}' already exists.");

            return await base.AddAsync(tag);
        }

        private static void ValidateTag(Tag tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            if (string.IsNullOrWhiteSpace(tag.Name))
                throw new ArgumentException("Tag name is required.", nameof(tag));

            if (tag.Name.Length > 50)
                throw new ArgumentException("Tag name cannot exceed 50 characters.", nameof(tag));
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Tag tag)
        {
            // Validation
            ValidateTag(tag);

            var existingTag = await _context.Tags.FindAsync(tag.Id) ?? throw new InvalidOperationException("Tag does not exist.");

            // Check for duplicate name (excluding current tag)
            var duplicateTag = await FirstOrDefaultAsync(t =>
                t.Id != tag.Id &&
                t.Name.ToLower() == tag.Name.ToLower());

            if (duplicateTag != null)
                throw new InvalidOperationException(
                    $"Tag '{tag.Name}' already exists.");

            existingTag.Name = tag.Name;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            var tag = await GetByIdAsync(id) ?? throw new InvalidOperationException("Tag does not exist.");

            // Delete the tag
            await base.DeleteAsync(tag);
        }

        #endregion

        #region Override GetAllAsync

        public override async Task<List<Tag>> GetAllAsync()
        {
            return await _dbSet.OrderBy(t => t.Name).ToListAsync();
        }

        #endregion
    }
}