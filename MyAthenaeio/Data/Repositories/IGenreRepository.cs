using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Data.Repositories
{
    public interface IGenreRepository : IRepository<Genre>
    {
        // Genre-specific queries
        Task<List<Genre>> SearchAsync(string query);
        Task<Genre?> GetByNameAsync(string name);

        // Book-related queries
        Task<List<Book>> GetBooksByGenreAsync(int genreId);
        Task<int> GetBookCountAsync(int genreId);
        Task<bool> HasBooksAsync(int genreId);
    }
}