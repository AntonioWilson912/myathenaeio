using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using Serilog;

namespace MyAthenaeio.Data.Repositories
{
    public class CollectionRepository(AppDbContext context) : Repository<Collection>(context), ICollectionRepository
    {
        private static readonly ILogger _logger = Log.ForContext<CollectionRepository>();

        #region Query Methods

        public async Task<List<Collection>> SearchAsync(string query)
        {
            query = query.ToLower();

            return await _dbSet
                .Where(c => c.Name.ToLower().Contains(query) ||
                            (c.Description != null && c.Description.ToLower().Contains(query)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Collection?> GetByNameAsync(string name)
        {
            return await FirstOrDefaultAsync(c => c.Name == name);
        }

        #endregion

        #region Book-Related Queries

        public async Task<List<Book>> GetBooksByCollectionAsync(int collectionId)
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .Include(b => b.Tags)
                .Include(b => b.Collections)
                .Where(b => b.Collections.Any(c => c.Id == collectionId))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<int> GetBookCountAsync(int collectionId)
        {
            return await _context.Books
                .CountAsync(b => b.Collections.Any(c => c.Id == collectionId));
        }

        public async Task<bool> HasBooksAsync(int collectionId)
        {
            return await _context.Books
                .AnyAsync(b => b.Collections.Any(c => c.Id == collectionId));
        }

        #endregion

        #region Add Methods with Validation

        public override async Task<Collection> AddAsync(Collection collection)
        {
            try
            {
                // Validation
                ValidateCollection(collection);

                _logger.Debug("Adding collection: {Name}", collection.Name);

                // Check for duplicate name
                var existingCollection = await FirstOrDefaultAsync(c =>
                    c.Name.ToLower() == collection.Name.ToLower());

                if (existingCollection != null)
                {
                    _logger.Warning("Cannot add collection - duplicate name: {Name}", collection.Name);
                    throw new InvalidOperationException(
                        $"Collection '{collection.Name}' already exists.");
                }

                var result = await base.AddAsync(collection);

                _logger.Information("Collection added: {Name} (ID: {Id})", collection.Name, collection.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add collection: {Name}", collection.Name);
                throw;
            }
        }

        private static void ValidateCollection(Collection collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            if (string.IsNullOrWhiteSpace(collection.Name))
                throw new ArgumentException("Collection name is required.", nameof(collection));

            if (collection.Name.Length > 50)
                throw new ArgumentException("Collection name cannot exceed 50 characters.", nameof(collection));
        }

        #endregion

        #region Update Methods

        public override async Task UpdateAsync(Collection collection)
        {
            try
            {
                ValidateCollection(collection);

                var existingCollection = await _context.Collections.FindAsync(collection.Id);

                if (existingCollection == null)
                {
                    _logger.Warning("Update failed: Collection {Id} not found", collection.Id);
                    throw new InvalidOperationException("Collection does not exist.");
                }

                _logger.Debug("Updating collection: {Name} (ID: {Id})", collection.Name, collection.Id);

                var duplicateCollection = await FirstOrDefaultAsync(c =>
                    c.Id != collection.Id &&
                    c.Name.ToLower() == collection.Name.ToLower());

                if (duplicateCollection != null)
                {
                    _logger.Warning("Update failed: duplicate name {Name} for collection {Id}",
                        collection.Name, collection.Id);
                    throw new InvalidOperationException(
                        $"Collection '{collection.Name}' already exists.");
                }

                existingCollection.Name = collection.Name;
                existingCollection.Description = collection.Description;
                existingCollection.Notes = collection.Notes;

                await _context.SaveChangesAsync();

                _logger.Information("Collection updated: {Name} (ID: {Id})", collection.Name, collection.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update collection ID: {Id}", collection.Id);
                throw;
            }
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            try
            {
                var collection = await GetByIdAsync(id);
                if (collection == null)
                {
                    _logger.Warning("Delete failed: Collection {Id} not found", id);
                    throw new InvalidOperationException("Collection does not exist.");
                }

                await base.DeleteAsync(collection);
                _logger.Information("Collection deleted: {Name} (ID: {Id})", collection.Name, id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete collection ID: {Id}", id);
            }
        }

        #endregion

        #region Override GetAllAsync

        public override async Task<List<Collection>> GetAllAsync()
        {
            return await _dbSet.OrderBy(c => c.Name).ToListAsync();
        }

        #endregion
    }
}