using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public class CollectionRepository(AppDbContext context) : Repository<Collection>(context), ICollectionRepository
    {

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
            // Validation
            ValidateCollection(collection);

            // Check for duplicate name
            var existingCollection = await FirstOrDefaultAsync(c =>
                c.Name.ToLower() == collection.Name.ToLower());

            if (existingCollection != null)
                throw new InvalidOperationException(
                    $"Collection '{collection.Name}' already exists.");

            return await base.AddAsync(collection);
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
            // Validation
            ValidateCollection(collection);

            var existingCollection = await GetByIdAsync(collection.Id) ?? throw new InvalidOperationException("Collection does not exist.");

            // Check for duplicate name (excluding current collection)
            var duplicateCollection = await FirstOrDefaultAsync(c =>
                c.Id != collection.Id &&
                c.Name.ToLower() == collection.Name.ToLower());

            if (duplicateCollection != null)
                throw new InvalidOperationException(
                    $"Collection '{collection.Name}' already exists.");

            existingCollection.Name = collection.Name;
            existingCollection.Description = collection.Description;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Delete Methods

        public override async Task DeleteAsync(int id)
        {
            var collection = await GetByIdAsync(id) ?? throw new InvalidOperationException("Collection does not exist.");

            // Now delete the collection
            await base.DeleteAsync(collection);
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