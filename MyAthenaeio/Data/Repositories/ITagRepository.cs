using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public interface ITagRepository : IRepository<Tag>
    {
        // Tag-specific queries
        Task<List<Tag>> SearchAsync(string query);
        Task<Tag?> GetByNameAsync(string name);

        // Book-related queries
        Task<List<Book>> GetBooksByTagAsync(int tagId);
        Task<int> GetBookCountAsync(int tagId);
        Task<bool> HasBooksAsync(int tagId);
    }
}