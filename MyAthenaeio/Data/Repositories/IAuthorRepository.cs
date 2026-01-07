using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Data.Repositories
{
    public interface IAuthorRepository : IRepository<Author>
    {
        // Override base methods with include options
        Task<Author?> GetByIdAsync(int id, AuthorIncludeOptions? options = null);
        Task<List<Author>> GetAllAsync(AuthorIncludeOptions? options = null);
        Task<List<Author>> GetAllAsNoTrackingAsync(AuthorIncludeOptions? options = null);

        // Author-specific queries
        Task<List<Author>> SearchAsync(string query, AuthorIncludeOptions? options = null);
        Task<List<Author>> GetByBookAsync(int bookId, AuthorIncludeOptions? options = null);
        Task<Author?> GetByNameAsync(string name);
        Task<Author?> GetByOpenLibraryKeyAsync(string openLibraryKey);

        // Author-specific operations
        Task<List<Book>> GetBooksByAuthorAsync(int authorId);
    }
}