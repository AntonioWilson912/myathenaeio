using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyAthenaeio.Models.DTOs;
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
                CoverImageUrl = b.CoverImageUrl
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
        public async Task<ImportResult> ImportFromFileAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var import = JsonConvert.DeserializeObject(json);

            if (import == null)
            {
                return new ImportResult
                {
                    Errors = new List<string> { "Invalid import file format" }
                };
            }

            return await ImportLibraryAsync(import);
        }

        public async Task<ImportResult> ImportLibraryAsync(object import)
        {
            return new ImportResult();
        }

        // Should be able to restore database (delete database, recreate it)

        // Should define DTOs in Models/DTOs
    }
}
