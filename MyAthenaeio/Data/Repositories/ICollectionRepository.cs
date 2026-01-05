using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public interface ICollectionRepository : IRepository<Collection>
    {
        // Collection-specific queries
        Task<List<Collection>> SearchAsync(string query);
        Task<Collection?> GetByNameAsync(string name);

        // Book-related queries
        Task<List<Book>> GetBooksByCollectionAsync(int collectionId);
        Task<int> GetBookCountAsync(int collectionId);
        Task<bool> HasBooksAsync(int collectionId);
    }
}