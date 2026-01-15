using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Data.Repositories
{
    public interface IBookRepository : IRepository<Book>
    {
        // Override base methods with include options
        Task<Book?> GetByIdAsync(int id, BookIncludeOptions? options = null);
        Task<List<Book>> GetAllAsync(BookIncludeOptions? options = null);
        Task<List<Book>> GetAllAsNoTrackingAsync(BookIncludeOptions? options = null);

        // Book-specific queries with include options
        Task<Book?> GetByISBNAsync(string isbn, BookIncludeOptions? options = null);
        Task<List<Book>> SearchAsync(string? query = null, int? authorId = null, int? genreId = null, int? tagId = null, int? collectionId = null, BookIncludeOptions? options = null);
        Task<List<Book>> GetByAuthorAsync(int authorId, BookIncludeOptions? options = null);
        Task<List<Book>> GetByGenreAsync(int genreId, BookIncludeOptions? options = null);
        Task<List<Book>> GetByTagAsync(int tagId, BookIncludeOptions? options = null);
        Task<List<Book>> GetByCollectionAsync(int collectionId, BookIncludeOptions? options = null);

        // Book-specific operations
        Task<Book> AddAsync(Book book, List<AuthorInfo> authorInfos,
            List<int>? genreIds = null, List<int>? tagIds = null, List<int>? collectionIds = null);

        // Availability
        Task<BookAvailability> GetAvailabilityAsync(int bookId);
        Task<int> GetAvailableCopiesAsync(int bookId);
        Task<bool> IsAvailableAsync(int bookId);

        // Relationship updates
        Task AddAuthorAsync(int bookId, int authorId);
        Task AddGenreAsync(int bookId, int genreId);
        Task AddTagAsync(int bookId, int tagId);
        Task AddCollectionAsync(int bookId, int collectionId);

        Task UpdateAuthorsAsync(int bookId, List<int> authorIds);
        Task UpdateGenresAsync(int bookId, List<int> genreIds);
        Task UpdateTagsAsync(int bookId, List<int> tagIds);
        Task UpdateCollectionsAsync(int bookId, List<int> collectionIds);

        Task RemoveGenreAsync(int bookId, int genreId);
        Task RemoveTagAsync(int bookId, int tagId);
        Task RemoveCollectionAsync(int bookId, int collectionId);
    }
}