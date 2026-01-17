using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using Serilog;

namespace MyAthenaeio.Data.Repositories
{
    public class TagRepository(AppDbContext context) : Repository<Tag>(context), ITagRepository
    {
        private static readonly ILogger _logger = Log.ForContext<TagRepository>();

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
            try
            {
                ValidateTag(tag);

                _logger.Debug("Adding tag: {Name}", tag.Name);

                var existingTag = await FirstOrDefaultAsync(t =>
                    t.Name.ToLower() == tag.Name.ToLower());

                if (existingTag != null)
                {
                    _logger.Warning("Cannot add tag - duplicate name: {Name}", tag.Name);
                    throw new InvalidOperationException(
                        $"Tag '{tag.Name}' already exists.");
                }

                var result = await base.AddAsync(tag);

                _logger.Information("Tag added: {Name} (ID: {Id})", tag.Name, tag.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add tag: {Name}", tag.Name);
                throw;
            }
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
            try
            {
                ValidateTag(tag);

                var existingTag = await _context.Tags.FindAsync(tag.Id);

                if (existingTag == null)
                {
                    _logger.Warning("Update failed: Tag {Id} not found", tag.Id);
                    throw new InvalidOperationException("Tag does not exist.");
                }

                _logger.Debug("Updating tag: {Name} (ID: {Id})", tag.Name, tag.Id);

                var duplicateTag = await FirstOrDefaultAsync(t =>
                    t.Id != tag.Id &&
                    t.Name.ToLower() == tag.Name.ToLower());

                if (duplicateTag != null)
                {
                    _logger.Warning("Update failed: duplicate name {Name} for tag {Id}",
                        tag.Name, tag.Id);
                    throw new InvalidOperationException(
                        $"Tag '{tag.Name}' already exists.");
                }

                existingTag.Name = tag.Name;

                await _context.SaveChangesAsync();

                _logger.Information("Tag updated: {Name} (ID: {Id})", tag.Name, tag.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update tag ID: {Id}", tag.Id);
                throw;
            }
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            try
            {
                var tag = await GetByIdAsync(id);
                if (tag == null)
                {
                    _logger.Warning("Delete failed: Tag {Id} not found", id);
                    throw new InvalidOperationException("Tag does not exist.");
                }

                await base.DeleteAsync(tag);
                _logger.Information("Tag deleted: {Name} (ID: {Id})", tag.Name, id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete tag ID: {Id}", id);
                throw;
            }
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