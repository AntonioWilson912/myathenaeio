using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Data;
using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using Newtonsoft.Json;

namespace MyAthenaeio.Services
{
    /// <summary>
    /// Provides functionality for managing Import/Export operations within the application.
    /// </summary>
    public class IMEXService
    {
        public static async Task<LibraryExportDTO> ExportToFileAsync(string filePath)
        {
            var export = await ExportLibraryAsync();
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(export, settings);
            await File.WriteAllTextAsync(filePath, json);

            return export;
        }

        private static async Task<LibraryExportDTO> ExportLibraryAsync()
        {
            var repos = new RepositoryFactory();
            var export = new LibraryExportDTO
            {
                ExportDate = DateTime.Now
            };

            // Export Books
            var books = await repos.Books.GetAllAsNoTrackingAsync();
            export.Books = [.. books.Select(b => new BookExportDTO
            {
                Id = b.Id,
                ISBN = b.ISBN,
                Title = b.Title,
                Subtitle = b.Subtitle,
                Description = b.Description,
                Publisher = b.Publisher,
                PublicationYear = b.PublicationYear,
                DateAdded = b.DateAdded,
                CoverImageUrl = b.CoverImageUrl,
                Notes = b.Notes
            })];

            // Export Authors
            var authors = await repos.Authors.GetAllAsNoTrackingAsync();
            export.Authors = [.. authors.Select(a => new AuthorExportDTO
            {
                Id = a.Id,
                Name = a.Name,
                OpenLibraryKey = a.OpenLibraryKey,
                Bio = a.Bio,
                BirthDate = a.BirthDate,
                PhotoUrl = a.PhotoUrl
            })];

            // Export Genres
            var genres = await repos.Genres.GetAllAsNoTrackingAsync();
            export.Genres = [.. genres.Select(g => new GenreExportDTO
            {
                Id = g.Id,
                Name = g.Name
            })];

            // Export Tags
            var tags = await repos.Tags.GetAllAsNoTrackingAsync();
            export.Tags = [.. tags.Select(t => new TagExportDTO
            {
                Id = t.Id,
                Name = t.Name
            })];

            // Export Collections
            var collections = await repos.Collections.GetAllAsNoTrackingAsync();
            export.Collections = [.. collections.Select(c => new CollectionExportDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Notes = c.Notes
            })];

            // Export BookAuthors relationships
            export.BookAuthors = [.. books.SelectMany(b =>
                b.Authors.Select(a => new BookAuthorExportDTO
                {
                    BookId = b.Id,
                    AuthorId = a.Id
                })
            )];

            // Export BookGenres relationships
            export.BookGenres = [.. books.SelectMany(b =>
                b.Genres.Select(g => new BookGenreExportDTO
                {
                    BookId = b.Id,
                    GenreId = g.Id
                })
            )];

            // Export BookTags relationships
            export.BookTags = [.. books.SelectMany(b =>
                b.Tags.Select(t => new BookTagExportDTO
                {
                    BookId = b.Id,
                    TagId = t.Id
                })
            )];

            // Export BookCollections relationships
            export.BookCollections = [.. books.SelectMany(b =>
                b.Collections.Select(c => new BookCollectionExportDTO
                {
                    BookId = b.Id,
                    CollectionId = c.Id
                })
            )];

            // Export BookCopies
            var bookCopies = await repos.BookCopies.GetAllAsNoTrackingAsync();
            export.BookCopies = [.. bookCopies.Select(bc => new BookCopyExportDTO
            {
                Id = bc.Id,
                BookId = bc.BookId,
                CopyNumber = bc.CopyNumber,
                AcquisitionDate = bc.AcquisitionDate,
                Condition = bc.Condition,
                IsAvailable = bc.IsAvailable,
                Notes = bc.Notes
            })];

            // Export Borrowers
            var borrowers = await repos.Borrowers.GetAllAsNoTrackingAsync();
            export.Borrowers = [.. borrowers.Select(b => new BorrowerExportDTO
            {
                Id = b.Id,
                Name = b.Name,
                Email = b.Email,
                Phone = b.Phone,
                DateAdded = b.DateAdded,
                IsActive = b.IsActive,
                Notes = b.Notes
            })];

            // Export Loans
            var loans = await repos.Loans.GetAllAsNoTrackingAsync();
            export.Loans = [.. loans.Select(l => new LoanExportDTO
            {
                Id = l.Id,
                BookId = l.BookId,
                BookCopyId = l.BookCopyId,
                BorrowerId = l.BorrowerId,
                CheckoutDate = l.CheckoutDate,
                EffectiveDueDate = l.EffectiveDueDate,
                ReturnDate = l.ReturnDate,
                MaxRenewalsAllowed = l.MaxRenewalsAllowed,
                LoanPeriodDays = l.LoanPeriodDays,
                Notes = l.Notes
            })];

            // Export Renewals
            export.Renewals = [.. loans.SelectMany(l =>
                l.Renewals.Select(r => new RenewalExportDTO
                {
                    Id = r.Id,
                    LoanId = l.Id,
                    RenewalDate = r.RenewalDate,
                    OldDueDate = r.OldDueDate,
                    NewDueDate = r.NewDueDate,
                    Notes = r.Notes
                })
            )];

            // Calculate statistics
            export.Statistics = new ExportStatistics
            {
                TotalBooks = export.Books.Count,
                TotalAuthors = export.Authors.Count,
                TotalGenres = export.Genres.Count,
                TotalTags = export.Tags.Count,
                TotalCollections = export.Collections.Count,
                TotalCopies = export.BookCopies.Count,
                TotalBorrowers = export.Borrowers.Count,
                ActiveLoans = export.Loans.Count(l => l.ReturnDate == null),
                CompletedLoans = export.Loans.Count(l => l.ReturnDate != null),
                TotalRenewals = export.Renewals.Count
            };
                
            return export;
        }

        // Should be able to import database from JSON
        public static async Task<ImportResult> ImportFromFileAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var import = JsonConvert.DeserializeObject<LibraryExportDTO>(json);

            if (import == null)
            {
                return new ImportResult
                {
                    Errors = new List<string> { "Invalid import file format" }
                };
            }

            return await ImportLibraryAsync(import);
        }

        public static async Task<ImportResult> ImportLibraryAsync(LibraryExportDTO import)
        {
            var result = new ImportResult();

            // Create ID mapping dictionaries for foreign key relationships
            var bookIdMap = new Dictionary<int, int>();
            var authorIdMap = new Dictionary<int, int>();
            var genreIdMap = new Dictionary<int, int>();
            var tagIdMap = new Dictionary<int, int>();
            var collectionIdMap = new Dictionary<int, int>();
            var borrowerIdMap = new Dictionary<int, int>();
            var bookCopyIdMap = new Dictionary<int, int>();
            var loanIdMap = new Dictionary<int, int>();

            try
            {
                // Import Books
                foreach (var bookDTO in import.Books)
                {
                    var existingBook = await LibraryService.GetBookByISBNAsync(bookDTO.ISBN);

                    if (existingBook != null)
                    {
                        bookIdMap[bookDTO.Id] = existingBook.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newBook = new Book
                        {
                            ISBN = bookDTO.ISBN,
                            Title = bookDTO.Title,
                            Subtitle = bookDTO.Subtitle,
                            Description = bookDTO.Description,
                            Publisher = bookDTO.Publisher,
                            PublicationYear = bookDTO.PublicationYear,
                            CoverImageUrl = bookDTO.CoverImageUrl,
                            DateAdded = bookDTO.DateAdded,
                            Notes = bookDTO.Notes
                        };

                        await LibraryService.AddBookAsync(newBook);
                        bookIdMap[bookDTO.Id] = newBook.Id;
                        result.BooksImported++;
                    }
                }

                // Import Authors
                foreach (var authorDTO in import.Authors)
                {
                    var existingAuthor = await LibraryService.GetAuthorByNameAsync(authorDTO.Name);
                    if (existingAuthor == null && !string.IsNullOrEmpty(authorDTO.OpenLibraryKey))
                    {
                        existingAuthor = await LibraryService.GetAuthorByOpenLibraryKeyAsync(authorDTO.OpenLibraryKey);
                    }

                    if (existingAuthor != null)
                    {
                        authorIdMap[authorDTO.Id] = existingAuthor.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newAuthor = new Author
                        {
                            Name = authorDTO.Name,
                            OpenLibraryKey = authorDTO.OpenLibraryKey,
                            Bio = authorDTO.Bio,
                            BirthDate = authorDTO.BirthDate,
                            PhotoUrl = authorDTO.PhotoUrl
                        };
                        await LibraryService.AddAuthorAsync(newAuthor);
                        authorIdMap[authorDTO.Id] = newAuthor.Id;
                        result.AuthorsImported++;
                    }
                }

                // Import Genres
                foreach (var genreDTO in import.Genres)
                {
                    var existingGenre = await LibraryService.GetGenreByNameAsync(genreDTO.Name);
                    if (existingGenre != null)
                    {
                        genreIdMap[genreDTO.Id] = existingGenre.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newGenre = new Genre
                        {
                            Name = genreDTO.Name
                        };
                        await LibraryService.AddGenreAsync(newGenre);
                        genreIdMap[genreDTO.Id] = newGenre.Id;
                        result.GenresImported++;
                    }
                }

                // Import Tags
                foreach (var tagDTO in import.Tags)
                {
                    var existingTag = await LibraryService.GetTagByNameAsync(tagDTO.Name);
                    if (existingTag != null)
                    {
                        tagIdMap[tagDTO.Id] = existingTag.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newTag = new Tag
                        {
                            Name = tagDTO.Name
                        };
                        await LibraryService.AddTagAsync(newTag);
                        tagIdMap[tagDTO.Id] = newTag.Id;
                        result.TagsImported++;
                    }
                }

                // Import Collections
                foreach (var collectionDTO in import.Collections)
                {
                    var existingCollection = await LibraryService.GetCollectionByNameAsync(collectionDTO.Name);
                    if (existingCollection != null)
                    {
                        collectionIdMap[collectionDTO.Id] = existingCollection.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newCollection = new Collection
                        {
                            Name = collectionDTO.Name,
                            Description = collectionDTO.Description,
                            Notes = collectionDTO.Notes
                        };
                        await LibraryService.AddCollectionAsync(newCollection);
                        collectionIdMap[collectionDTO.Id] = newCollection.Id;
                        result.CollectionsImported++;
                    }
                }

                // Import BookAuthor relationships
                foreach (var bookAuthorDTO in import.BookAuthors)
                {
                    if (bookIdMap.TryGetValue(bookAuthorDTO.BookId, out var mappedBookId) &&
                        authorIdMap.TryGetValue(bookAuthorDTO.AuthorId, out var mappedAuthorId))
                    {
                        await LibraryService.AddAuthorToBookAsync(mappedBookId, mappedAuthorId);
                    }
                }

                // Import BookGenre relationships
                foreach (var bookGenreDTO in import.BookGenres)
                {
                    if (bookIdMap.TryGetValue(bookGenreDTO.BookId, out var mappedBookId) &&
                        genreIdMap.TryGetValue(bookGenreDTO.GenreId, out var mappedGenreId))
                    {
                        await LibraryService.AddGenreToBookAsync(mappedBookId, mappedGenreId);
                    }
                }

                // Import BookTag relationships
                foreach (var bookTagDTO in import.BookTags)
                {
                    if (bookIdMap.TryGetValue(bookTagDTO.BookId, out var mappedBookId) &&
                        tagIdMap.TryGetValue(bookTagDTO.TagId, out var mappedTagId))
                    {
                        await LibraryService.AddTagToBookAsync(mappedBookId, mappedTagId);
                    }
                }

                // Import BookCollection relationships
                foreach (var bookCollectionDTO in import.BookCollections)
                {
                    if (bookIdMap.TryGetValue(bookCollectionDTO.BookId, out var mappedBookId) &&
                        collectionIdMap.TryGetValue(bookCollectionDTO.CollectionId, out var mappedCollectionId))
                    {
                        await LibraryService.AddCollectionToBookAsync(mappedBookId, mappedCollectionId);
                    }
                }

                // Import Borrowers
                foreach (var borrowerDTO in import.Borrowers)
                {
                    var existingBorrower = await LibraryService.GetBorrowerByNameAsync(borrowerDTO.Name);
                    if (existingBorrower != null)
                    {
                        borrowerIdMap[borrowerDTO.Id] = existingBorrower.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newBorrower = new Borrower
                        {
                            Name = borrowerDTO.Name,
                            Email = borrowerDTO.Email,
                            Phone = borrowerDTO.Phone,
                            DateAdded = borrowerDTO.DateAdded,
                            IsActive = borrowerDTO.IsActive,
                            Notes = borrowerDTO.Notes
                        };
                        await LibraryService.AddBorrowerAsync(newBorrower);
                        borrowerIdMap[borrowerDTO.Id] = newBorrower.Id;
                        result.BorrowersImported++;
                    }
                }

                // Import BookCopies
                foreach (var bookCopyDTO in import.BookCopies)
                {
                    var mappedBookId = bookIdMap[bookCopyDTO.BookId];
                    var existingCopies = await LibraryService.GetBookCopiesAsync(mappedBookId);
                    var duplicateCopy = existingCopies.FirstOrDefault(bc => bc.CopyNumber == bookCopyDTO.CopyNumber);
                    if (duplicateCopy != null)
                    {
                        bookCopyIdMap[bookCopyDTO.Id] = duplicateCopy.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newBookCopy = new BookCopy
                        {
                            BookId = mappedBookId,
                            CopyNumber = bookCopyDTO.CopyNumber,
                            AcquisitionDate = bookCopyDTO.AcquisitionDate,
                            Condition = bookCopyDTO.Condition,
                            IsAvailable = bookCopyDTO.IsAvailable,
                            Notes = bookCopyDTO.Notes
                        };
                        await LibraryService.AddBookCopyAsync(mappedBookId, newBookCopy);
                        bookCopyIdMap[bookCopyDTO.Id] = newBookCopy.Id;
                        result.CopiesImported++;
                    }
                }

                // Import Loans
                foreach (var loanDTO in import.Loans)
                {
                    var mappedBookId = bookIdMap[loanDTO.BookId];
                    var mappedBookCopyId = bookCopyIdMap[loanDTO.BookCopyId];
                    var mappedBorrowerId = borrowerIdMap[loanDTO.BorrowerId];

                    // Check for exact duplicate: same book copy, borrower, and checkout date
                    var existingLoan = await LibraryService.GetLoanByDetailsAsync(
                        mappedBookCopyId,
                        mappedBorrowerId,
                        loanDTO.CheckoutDate
                    );

                    if (existingLoan != null)
                    {
                        loanIdMap[loanDTO.Id] = existingLoan.Id;
                        result.ItemsSkipped++;
                    }
                    else
                    {
                        var newLoan = new Loan
                        {
                            BookId = mappedBookId,
                            BookCopyId = mappedBookCopyId,
                            BorrowerId = mappedBorrowerId,
                            CheckoutDate = loanDTO.CheckoutDate,
                            ReturnDate = loanDTO.ReturnDate,
                            MaxRenewalsAllowed = loanDTO.MaxRenewalsAllowed,
                            LoanPeriodDays = loanDTO.LoanPeriodDays,
                            Notes = loanDTO.Notes
                        };
                        await LibraryService.AddLoanAsync(newLoan);
                        loanIdMap[loanDTO.Id] = newLoan.Id;
                        result.LoansImported++;
                    }
                }

                // Import Renewals
                foreach (var renewalDTO in import.Renewals)
                {
                    var mappedLoanId = loanIdMap[renewalDTO.LoanId];
                    var newRenewal = new Renewal
                    {
                        LoanId = mappedLoanId,
                        RenewalDate = renewalDTO.RenewalDate,
                        OldDueDate = renewalDTO.OldDueDate,
                        NewDueDate = renewalDTO.NewDueDate,
                        Notes = renewalDTO.Notes
                    };
                    await LibraryService.AddRenewalAsync(newRenewal);
                    result.RenewalsImported++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Import error: {ex.Message}");
                Debug.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception error: {ex.InnerException.Message}");
                }
            }
            
            return result;
        }

        public static async Task ResetDatabaseAsync()
        {
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(100); // Small delay to ensure all connections are closed

            // Create safety backup in case reset does not work
            var safetyBackupPath = AppDbContext.DbPath + ".safety";
            File.Copy(AppDbContext.DbPath, safetyBackupPath, overwrite: true);

            try
            {
                // Delete existing database file
                if (File.Exists(AppDbContext.DbPath))
                {
                    File.Delete(AppDbContext.DbPath);
                }

                // Recreate the database
                using (var context = new AppDbContext())
                {
                    await context.Database.MigrateAsync();
                }

                // Success - delete safety backup
                File.Delete(safetyBackupPath);

            }
            catch (Exception ex)
            {
                // Restore from safety backup
                if (File.Exists(safetyBackupPath))
                {
                    File.Copy(safetyBackupPath, AppDbContext.DbPath, overwrite: true);
                    File.Delete(safetyBackupPath);
                }
                throw new InvalidOperationException($"Failed to reset database: {ex.Message}");
            }
        }

        public static async Task RestoreDatabaseAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Backup file not found", backupFilePath);
            }

            // Close all database connections
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(100); // Small delay to ensure all connections are closed

            // Create safety backup
            var safetyBackupPath = AppDbContext.DbPath + ".safety";
            File.Copy(AppDbContext.DbPath, safetyBackupPath, overwrite: true);

            try
            {
                // Restore the backup
                File.Copy(backupFilePath, AppDbContext.DbPath, overwrite: true);
                
                // Validate it works
                using (var context = new AppDbContext())
                {
                    if (!await context.Database.CanConnectAsync())
                    {
                        throw new InvalidOperationException("Cannot connect to the restored database.");
                    }
                    

                    // Sanity check
                    _ = await context.Books.CountAsync();
                }

                // Success
                File.Delete(safetyBackupPath);
            } catch (Exception ex)
            {
                // Restore from safety backup
                if (File.Exists(safetyBackupPath))
                {
                    File.Copy(safetyBackupPath, AppDbContext.DbPath, overwrite: true);
                    File.Delete(safetyBackupPath);
                }
                throw new InvalidOperationException($"Failed to restore database: {ex.Message}");
            }
        }
    }
}
