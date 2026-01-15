using MyAthenaeio.Data;
using MyAthenaeio.Data.Repositories;
using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using System.CodeDom;
using System.Diagnostics;

namespace MyAthenaeio.Services
{
    /// <summary>
    /// Thin facade over repository layer. Provides a simplified API for data operations.
    /// All validation and business logic is handled in the repository layer.
    /// </summary>
    public static class LibraryService
    {
        #region Authors

        /// <summary>
        /// Adds a new author to the library.
        /// </summary>
        public static async Task<Author> AddAuthorAsync(Author author)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.AddAsync(author);
        }

        /// <summary>
        /// Convenience method to add an author by name and bio.
        /// </summary>
        public static async Task<Author> AddAuthorAsync(string name, string? bio = null, string? openLibraryKey = null)
        {
            var author = new Author
            {
                Name = name,
                Bio = bio,
                OpenLibraryKey = openLibraryKey
            };
            return await AddAuthorAsync(author);
        }

        /// <summary>
        /// Gets all authors.
        /// </summary>
        public static async Task<List<Author>> GetAllAuthorsAsync(AuthorIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.GetAllAsync(options);
        }

        /// <summary>
        /// Gets an author by ID.
        /// </summary>
        public static async Task<Author?> GetAuthorByIdAsync(int id, AuthorIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.GetByIdAsync(id, options);
        }

        /// <summary>
        /// Gets an author by name.
        /// </summary>
        public static async Task<Author?> GetAuthorByNameAsync(string name)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.GetByNameAsync(name);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<List<Author>> GetAuthorsByBookAsync(int _bookId,  AuthorIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.GetByBookAsync(_bookId, options);
        }

        /// <summary>
        /// Gets an author by OpenLibrary key.
        /// </summary>
        public static async Task<Author?> GetAuthorByOpenLibraryKeyAsync(string openLibraryKey)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.GetByOpenLibraryKeyAsync(openLibraryKey);
        }

        /// <summary>
        /// Searches for authors by name or bio.
        /// </summary>
        public static async Task<List<Author>> SearchAuthorsAsync(string query, AuthorIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.SearchAsync(query, options);
        }

        /// <summary>
        /// Updates an author's information.
        /// </summary>
        public static async Task UpdateAuthorAsync(Author author)
        {
            using var repos = new RepositoryFactory();
            await repos.Authors.UpdateAsync(author);
        }

        /// <summary>
        /// Deletes an author.
        /// </summary>
        public static async Task DeleteAuthorAsync(int id)
        {
            using var repos = new RepositoryFactory();
            await repos.Authors.DeleteAsync(id);
        }

        /// <summary>
        /// Gets all books by an author.
        /// </summary>
        public static async Task<List<Book>> GetBooksByAuthorAsync(int authorId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Authors.GetBooksByAuthorAsync(authorId);
        }

        #endregion

        #region Books

        /// <summary>
        /// Adds a new book to the library.
        /// </summary>
        public static async Task<Book> AddBookAsync(Book book)
        {
            return await AddBookAsync(book, []);
        }

        /// <summary>
        /// Adds a new book to the library with authors and optional categorization.
        /// </summary>
        public static async Task<Book> AddBookAsync(
            Book book,
            List<AuthorInfo> authorInfos,
            List<int>? genreIds = null,
            List<int>? tagIds = null,
            List<int>? collectionIds = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.AddAsync(book, authorInfos, genreIds, tagIds, collectionIds);
        }

        /// <summary>
        /// Adds an author to a book.
        /// </summary>
        public static async Task AddAuthorToBookAsync(int bookId, int genreId)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.AddAuthorAsync(bookId, genreId);
        }

        /// <summary>
        /// Adds a genre to a book
        /// </summary>
        public static async Task AddGenreToBookAsync(int bookId, int genreId)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.AddGenreAsync(bookId, genreId);
        }

        /// <summary>
        /// Adds a tag to a book
        /// </summary>
        public static async Task AddTagToBookAsync(int bookId, int tagId)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.AddTagAsync(bookId, tagId);
        }

        /// <summary>
        /// Adds a collection to a book
        /// </summary>
        public static async Task AddCollectionToBookAsync(int bookId, int collectionId)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.AddCollectionAsync(bookId, collectionId);
        }


        /// <summary>
        /// Gets all books in the library.
        /// </summary>
        public static async Task<List<Book>> GetAllBooksAsync(BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetAllAsync(options);
        }

        /// <summary>
        /// Gets a book by ID.
        /// </summary>
        public static async Task<Book?> GetBookByIdAsync(int id, BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetByIdAsync(id, options);
        }

        /// <summary>
        /// Gets a book by ISBN (supports both ISBN-10 and ISBN-13).
        /// </summary>
        public static async Task<Book?> GetBookByISBNAsync(string isbn, BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetByISBNAsync(isbn, options);
        }

        /// <summary>
        /// Searches for books by title, author, ISBN, or description.
        /// </summary>
        public static async Task<List<Book>> SearchBooksAsync(string? query = null,
            int? authorId = null,
            int? genreId = null,
            int? tagId = null,
            int? collectionId = null,
            BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.SearchAsync(query, authorId, genreId, tagId, collectionId, options);
        }

        /// <summary>
        /// Gets all books by an author.
        /// </summary>
        public static async Task<List<Book>> GetBooksByAuthorAsync(Author author)
            => await GetBooksByAuthorAsync(author.Id);

        /// <summary>
        /// Gets all books by an author ID.
        /// </summary>
        public static async Task<List<Book>> GetBooksByAuthorAsync(int authorId, BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetByAuthorAsync(authorId, options);
        }

        /// <summary>
        /// Gets all books in a genre.
        /// </summary>
        public static async Task<List<Book>> GetBooksByGenreAsync(Genre genre)
            => await GetBooksByGenreAsync(genre.Id);

        /// <summary>
        /// Gets all books in a genre by ID.
        /// </summary>
        public static async Task<List<Book>> GetBooksByGenreAsync(int genreId, BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetByGenreAsync(genreId, options);
        }

        /// <summary>
        /// Gets all books with a tag.
        /// </summary>
        public static async Task<List<Book>> GetBooksByTagAsync(Tag tag)
            => await GetBooksByTagAsync(tag.Id);

        /// <summary>
        /// Gets all books with a tag by ID.
        /// </summary>
        public static async Task<List<Book>> GetBooksByTagAsync(int tagId, BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetByTagAsync(tagId, options);
        }

        /// <summary>
        /// Gets all books in a collection.
        /// </summary>
        public static async Task<List<Book>> GetBooksByCollectionAsync(Collection collection)
            => await GetBooksByCollectionAsync(collection.Id);

        /// <summary>
        /// Gets all books in a collection by ID.
        /// </summary>
        public static async Task<List<Book>> GetBooksByCollectionAsync(int collectionId, BookIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetByCollectionAsync(collectionId, options);
        }

        /// <summary>
        /// Updates book properties (not relationships).
        /// </summary>
        public static async Task UpdateBookAsync(Book book)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.UpdateAsync(book);
        }

        /// <summary>
        /// Updates a book's authors.
        /// </summary>
        public static async Task UpdateBookAuthorsAsync(int bookId, List<int> authorIds)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.UpdateAuthorsAsync(bookId, authorIds);
        }

        /// <summary>
        /// Updates a book's genres.
        /// </summary>
        public static async Task UpdateBookGenresAsync(int bookId, List<int> genreIds)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.UpdateGenresAsync(bookId, genreIds);
        }

        /// <summary>
        /// Updates a book's tags.
        /// </summary>
        public static async Task UpdateBookTagsAsync(int bookId, List<int> tagIds)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.UpdateTagsAsync(bookId, tagIds);
        }

        /// <summary>
        /// Updates a book's collections.
        /// </summary>
        public static async Task UpdateBookCollectionsAsync(int bookId, List<int> collectionIds)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.UpdateCollectionsAsync(bookId, collectionIds);
        }

        /// <summary>
        /// Removes a genre from a book
        /// </summary>
        public static async Task RemoveGenreFromBookAsync(int bookId, int genreId)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.RemoveGenreAsync(bookId, genreId);
        }

        /// <summary>
        /// Removes a tag from a book
        /// </summary>
        public static async Task RemoveTagFromBookAsync(int bookId, int tagId)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.RemoveTagAsync(bookId, tagId);
        }

        /// <summary>
        /// Removes a collection from a book
        /// </summary>
        public static async Task RemoveCollectionFromBookAsync(int bookId, int collectionId)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.RemoveCollectionAsync(bookId, collectionId);
        }

        /// <summary>
        /// Deletes a book from the library.
        /// </summary>
        public static async Task DeleteBookAsync(int id)
        {
            using var repos = new RepositoryFactory();
            await repos.Books.DeleteAsync(id);
        }

        #endregion

        #region Book Availability

        /// <summary>
        /// Gets detailed availability information for a book.
        /// </summary>
        public static async Task<BookAvailability> GetBookAvailabilityAsync(int bookId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetAvailabilityAsync(bookId);
        }

        /// <summary>
        /// Gets the number of available copies of a book.
        /// </summary>
        public static async Task<int> GetAvailableCopiesAsync(int bookId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.GetAvailableCopiesAsync(bookId);
        }

        /// <summary>
        /// Checks if a book has any available copies.
        /// </summary>
        public static async Task<bool> IsBookAvailableAsync(int bookId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Books.IsAvailableAsync(bookId);
        }

        #endregion

        #region Book Copies

        /// <summary>
        /// Adds a new copy of a book.
        /// </summary>
        public static async Task<BookCopy> AddBookCopyAsync(int bookId, BookCopy copy)
        {
            using var repos = new RepositoryFactory();
            return await repos.BookCopies.AddCopyAsync(bookId, copy);
        }

        /// <summary>
        /// Gets all copies of a book.
        /// </summary>
        public static async Task<List<BookCopy>> GetBookCopiesAsync(int bookId)
        {
            using var repos = new RepositoryFactory();
            return await repos.BookCopies.GetByBookAsync(bookId);
        }

        /// <summary>
        /// Gets all available copies of a book.
        /// </summary>
        public static async Task<List<BookCopy>> GetAvailableBookCopiesAsync(int bookId)
        {
            using var repos = new RepositoryFactory();
            return await repos.BookCopies.GetAvailableCopiesAsync(bookId);
        }

        /// <summary>
        /// Gets a book copy by ID.
        /// </summary>
        public static async Task<BookCopy?> GetBookCopyByIdAsync(int id, BookCopyIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.BookCopies.GetByIdAsync(id, options);
        }

        /// <summary>
        /// Updates a book copy's information.
        /// </summary>
        public static async Task UpdateBookCopyAsync(BookCopy bookCopy)
        {
            using var repos = new RepositoryFactory();
            await repos.BookCopies.UpdateAsync(bookCopy);
        }

        /// <summary>
        /// Marks a book copy as unavailable (lost, damaged, etc.).
        /// </summary>
        public static async Task MarkBookCopyUnavailableAsync(int bookCopyId)
        {
            using var repos = new RepositoryFactory();
            await repos.BookCopies.MarkAsUnavailableAsync(bookCopyId);
        }

        /// <summary>
        /// Marks a book copy as available.
        /// </summary>
        public static async Task MarkBookCopyAvailableAsync(int bookCopyId)
        {
            using var repos = new RepositoryFactory();
            await repos.BookCopies.MarkAsAvailableAsync(bookCopyId);
        }

        /// <summary>
        /// Deletes a book copy.
        /// </summary>
        public static async Task DeleteBookCopyAsync(int id)
        {
            using var repos = new RepositoryFactory();
            await repos.BookCopies.DeleteAsync(id);
        }

        #endregion

        #region Borrowers

        /// <summary>
        /// Adds a new borrower.
        /// </summary>
        public static async Task<Borrower> AddBorrowerAsync(Borrower borrower)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.AddAsync(borrower);
        }

        /// <summary>
        /// Convenience method to add a borrower by details.
        /// </summary>
        public static async Task<Borrower> AddBorrowerAsync(string name, string? email = null, string? phone = null, string? notes = null)
        {
            var borrower = new Borrower
            {
                Name = name,
                Email = email,
                Phone = phone,
                Notes = notes,
                DateAdded = DateTime.UtcNow
            };
            return await AddBorrowerAsync(borrower);
        }

        /// <summary>
        /// Gets all borrowers.
        /// </summary>
        public static async Task<List<Borrower>> GetAllBorrowersAsync(BorrowerIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetAllAsync(options);
        }

        /// <summary>
        /// Gets all active borrowers.
        /// </summary>
        public static async Task<List<Borrower>> GetActiveBorrowersAsync(BorrowerIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetAllActiveAsync(options);
        }

        /// <summary>
        /// Gets a borrower by ID.
        /// </summary>
        public static async Task<Borrower?> GetBorrowerByIdAsync(int id, BorrowerIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetByIdAsync(id, options);
        }

        /// <summary>
        /// Gets a borrower by name.
        /// </summary>
        public static async Task<Borrower?> GetBorrowerByNameAsync(string name, BorrowerIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetByNameAsync(name, options);
        }

        /// <summary>
        /// Gets a borrower by email.
        /// </summary>
        public static async Task<Borrower?> GetBorrowerByEmailAsync(string email)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetByEmailAsync(email);
        }

        /// <summary>
        /// Gets a borrower by phone number.
        /// </summary>
        public static async Task<Borrower?> GetBorrowerByPhoneAsync(string phone)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetByPhoneAsync(phone);
        }

        /// <summary>
        /// Searches for borrowers by name, email, or phone.
        /// </summary>
        public static async Task<List<Borrower>> SearchBorrowersAsync(string query, BorrowerIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.SearchAsync(query, options);
        }

        /// <summary>
        /// Updates a borrower's information.
        /// </summary>
        public static async Task UpdateBorrowerAsync(Borrower borrower)
        {
            using var repos = new RepositoryFactory();
            await repos.Borrowers.UpdateAsync(borrower);
        }

        /// <summary>
        /// Deletes a borrower.
        /// </summary>
        public static async Task DeleteBorrowerAsync(int id)
        {
            using var repos = new RepositoryFactory();
            await repos.Borrowers.DeleteAsync(id);
        }

        /// <summary>
        /// Checks if a borrower has any active loans.
        /// </summary>
        public static async Task<bool> HasActiveLoansAsync(int borrowerId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.HasActiveLoansAsync(borrowerId);
        }

        /// <summary>
        /// Gets the number of active loans for a borrower.
        /// </summary>
        public static async Task<int> GetActiveLoanCountAsync(int borrowerId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetActiveLoanCountAsync(borrowerId);
        }

        /// <summary>
        /// Gets all loans for a borrower.
        /// </summary>
        public static async Task<List<Loan>> GetLoansByBorrowerAsync(int borrowerId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetLoansByBorrowerAsync(borrowerId);
        }

        /// <summary>
        /// Gets all active loans for a borrower.
        /// </summary>
        public static async Task<List<Loan>> GetActiveLoansByBorrowerAsync(int borrowerId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Borrowers.GetActiveLoansByBorrowerAsync(borrowerId);
        }

        #endregion

        #region Collections

        /// <summary>
        /// Adds a new collection.
        /// </summary>
        public static async Task<Collection> AddCollectionAsync(Collection collection)
        {
            using var repos = new RepositoryFactory();
            return await repos.Collections.AddAsync(collection);
        }

        /// <summary>
        /// Convenience method to add a collection by name and description.
        /// </summary>
        public static async Task<Collection> AddCollectionAsync(string name, string? description = null, string? notes = null)
        {
            var collection = new Collection
            {
                Name = name,
                Description = description,
                Notes = notes
            };
            return await AddCollectionAsync(collection);
        }

        /// <summary>
        /// Gets all collections.
        /// </summary>
        public static async Task<List<Collection>> GetAllCollectionsAsync()
        {
            using var repos = new RepositoryFactory();
            return await repos.Collections.GetAllAsync();
        }

        /// <summary>
        /// Gets a collection by ID.
        /// </summary>
        public static async Task<Collection?> GetCollectionByIdAsync(int id)
        {
            using var repos = new RepositoryFactory();
            return await repos.Collections.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets a collection by name.
        /// </summary>
        public static async Task<Collection?> GetCollectionByNameAsync(string name)
        {
            using var repos = new RepositoryFactory();
            return await repos.Collections.GetByNameAsync(name);
        }

        /// <summary>
        /// Searches for collections by name or description.
        /// </summary>
        public static async Task<List<Collection>> SearchCollectionsAsync(string query)
        {
            using var repos = new RepositoryFactory();
            return await repos.Collections.SearchAsync(query);
        }

        /// <summary>
        /// Updates a collection.
        /// </summary>
        public static async Task UpdateCollectionAsync(Collection collection)
        {
            using var repos = new RepositoryFactory();
            await repos.Collections.UpdateAsync(collection);
        }

        /// <summary>
        /// Deletes a collection.
        /// </summary>
        public static async Task DeleteCollectionAsync(int id)
        {
            using var repos = new RepositoryFactory();
            await repos.Collections.DeleteAsync(id);
        }

        /// <summary>
        /// Gets the number of books in a collection.
        /// </summary>
        public static async Task<int> GetBookCountInCollectionAsync(int collectionId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Collections.GetBookCountAsync(collectionId);
        }

        #endregion

        #region Genres

        /// <summary>
        /// Adds a new genre.
        /// </summary>
        public static async Task<Genre> AddGenreAsync(Genre genre)
        {
            using var repos = new RepositoryFactory();
            return await repos.Genres.AddAsync(genre);
        }

        /// <summary>
        /// Convenience method to add a genre by name and description.
        /// </summary>
        public static async Task<Genre> AddGenreAsync(string name, string? description = null)
        {
            var genre = new Genre
            {
                Name = name
            };
            return await AddGenreAsync(genre);
        }

        /// <summary>
        /// Gets all genres.
        /// </summary>
        public static async Task<List<Genre>> GetAllGenresAsync()
        {
            using var repos = new RepositoryFactory();
            return await repos.Genres.GetAllAsync();
        }

        /// <summary>
        /// Gets a genre by ID.
        /// </summary>
        public static async Task<Genre?> GetGenreByIdAsync(int id)
        {
            using var repos = new RepositoryFactory();
            return await repos.Genres.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets a genre by name.
        /// </summary>
        public static async Task<Genre?> GetGenreByNameAsync(string name)
        {
            using var repos = new RepositoryFactory();
            return await repos.Genres.GetByNameAsync(name);
        }

        /// <summary>
        /// Searches for genres by name.
        /// </summary>
        public static async Task<List<Genre>> SearchGenresAsync(string query)
        {
            using var repos = new RepositoryFactory();
            return await repos.Genres.SearchAsync(query);
        }

        /// <summary>
        /// Updates a genre.
        /// </summary>
        public static async Task UpdateGenreAsync(Genre genre)
        {
            using var repos = new RepositoryFactory();
            await repos.Genres.UpdateAsync(genre);
        }

        /// <summary>
        /// Deletes a genre.
        /// </summary>
        public static async Task DeleteGenreAsync(int id)
        {
            using var repos = new RepositoryFactory();
            await repos.Genres.DeleteAsync(id);
        }

        /// <summary>
        /// Gets the number of books in a genre.
        /// </summary>
        public static async Task<int> GetBookCountInGenreAsync(int genreId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Genres.GetBookCountAsync(genreId);
        }

        #endregion

        #region Loans

        /// <summary>
        /// Adds a new loan.
        /// </summary>
        public static async Task<Loan> AddLoanAsync(Loan loan)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.AddAsync(loan);
        }

        public static async Task<Renewal> AddRenewalAsync(Renewal renewal)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.AddRenewalAsync(renewal);
        }

        /// <summary>
        /// Checks out a book to a borrower.
        /// </summary>
        public static async Task<Loan> CheckoutBookAsync(int bookCopyId, int borrowerId, int maxRenewals = 2, int loanPeriodDays = 14)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.CheckoutAsync(bookCopyId, borrowerId, maxRenewals, loanPeriodDays);
        }

        /// <summary>
        /// Returns a loaned book.
        /// </summary>
        public static async Task<Loan> ReturnBookAsync(int loanId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.ReturnAsync(loanId);
        }

        /// <summary>
        /// Renews a loan.
        /// </summary>
        public static async Task<Renewal> RenewLoanAsync(int loanId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.RenewAsync(loanId);
        }

        /// <summary>
        /// Gets all loans.
        /// </summary>
        public static async Task<List<Loan>> GetAllLoansAsync(LoanIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetAllAsync(options);
        }

        public static async Task<Loan?> GetLoanByDetailsAsync(int bookCopyId, int borrowerId, DateTime checkoutDate)
        {
            var loans = await GetAllLoansAsync();
            return loans.FirstOrDefault(l =>
                l.BookCopyId == bookCopyId &&
                l.BorrowerId == borrowerId &&
                l.CheckoutDate.Date == checkoutDate.Date);
        }

        /// <summary>
        /// Gets all active (unreturned) loans.
        /// </summary>
        public static async Task<List<Loan>> GetActiveLoansAsync(LoanIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetActiveLoansAsync(options);
        }

        /// <summary>
        /// Gets all overdue loans.
        /// </summary>
        public static async Task<List<Loan>> GetOverdueLoansAsync(LoanIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetOverdueLoansAsync(options);
        }

        /// <summary>
        /// Gets loans due within the specified number of days.
        /// </summary>
        public static async Task<List<Loan>> GetDueSoonAsync(int daysAhead = 7, LoanIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetDueSoonAsync(daysAhead, options);
        }

        /// <summary>
        /// Gets all loans for a specific book.
        /// </summary>
        public static async Task<List<Loan>> GetLoansByBookAsync(int bookId, LoanIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetByBookAsync(bookId, options);
        }

        /// <summary>
        /// Gets a loan by ID.
        /// </summary>
        public static async Task<Loan?> GetLoanByIdAsync(int id, LoanIncludeOptions? options = null)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetByIdAsync(id, options);
        }

        /// <summary>
        /// Gets the effective due date for a loan (accounting for renewals).
        /// </summary>
        public static async Task<DateTime> GetEffectiveDueDateAsync(int loanId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetEffectiveDueDateAsync(loanId);
        }

        /// <summary>
        /// Checks if a loan is overdue.
        /// </summary>
        public static async Task<bool> IsLoanOverdueAsync(int loanId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.IsOverdueAsync(loanId);
        }

        /// <summary>
        /// Gets the number of days a loan is overdue.
        /// </summary>
        public static async Task<int> GetDaysOverdueAsync(int loanId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Loans.GetDaysOverdueAsync(loanId);
        }

        /// <summary>
        /// Updates a loan entity.
        /// </summary>
        public static async Task UpdateLoanAsync(Loan loan)
        {
            using var repos = new RepositoryFactory();
            await repos.Loans.UpdateAsync(loan);
        }

        #endregion

        #region Tags

        /// <summary>
        /// Adds a new tag.
        /// </summary>
        public static async Task<Tag> AddTagAsync(Tag tag)
        {
            using var repos = new RepositoryFactory();
            return await repos.Tags.AddAsync(tag);
        }

        /// <summary>
        /// Convenience method to add a tag by name.
        /// </summary>
        public static async Task<Tag> AddTagAsync(string name)
        {
            var tag = new Tag { Name = name };
            return await AddTagAsync(tag);
        }

        /// <summary>
        /// Gets all tags.
        /// </summary>
        public static async Task<List<Tag>> GetAllTagsAsync()
        {
            using var repos = new RepositoryFactory();
            return await repos.Tags.GetAllAsync();
        }

        /// <summary>
        /// Gets a tag by ID.
        /// </summary>
        public static async Task<Tag?> GetTagByIdAsync(int id)
        {
            using var repos = new RepositoryFactory();
            return await repos.Tags.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets a tag by name.
        /// </summary>
        public static async Task<Tag?> GetTagByNameAsync(string name)
        {
            using var repos = new RepositoryFactory();
            return await repos.Tags.GetByNameAsync(name);
        }

        /// <summary>
        /// Searches for tags by name.
        /// </summary>
        public static async Task<List<Tag>> SearchTagsAsync(string query)
        {
            using var repos = new RepositoryFactory();
            return await repos.Tags.SearchAsync(query);
        }

        /// <summary>
        /// Updates a tag.
        /// </summary>
        public static async Task UpdateTagAsync(Tag tag)
        {
            using var repos = new RepositoryFactory();
            await repos.Tags.UpdateAsync(tag);
        }

        /// <summary>
        /// Deletes a tag.
        /// </summary>
        public static async Task DeleteTagAsync(int id)
        {
            using var repos = new RepositoryFactory();
            await repos.Tags.DeleteAsync(id);
        }

        /// <summary>
        /// Gets the number of books with a tag.
        /// </summary>
        public static async Task<int> GetBookCountWithTagAsync(int tagId)
        {
            using var repos = new RepositoryFactory();
            return await repos.Tags.GetBookCountAsync(tagId);
        }

        #endregion
    }
}