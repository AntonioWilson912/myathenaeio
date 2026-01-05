using System.IO;
using Microsoft.EntityFrameworkCore;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Utils;


namespace MyAthenaeio.Data
{
    public class AppDbContext : DbContext
    {
        // DbSets for tables
        public DbSet<Book> Books { get; set; }
        public DbSet<BookCopy> BookCopies { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Borrower> Borrowers { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Renewal> Renewals { get; set; }
        public DbSet<Collection> Collections { get; set; }

        // Database file path
        public static string DbPath { get; private set; }

        static AppDbContext()
        {
            // Set up database path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "myAthenaeio");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(appFolder);

            DbPath = Path.Combine(appFolder, "library.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Book entity
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ISBN).IsUnique();
                entity.HasIndex(e => e.Title);

                entity.Property(e => e.ISBN)
                    .IsRequired()
                    .HasMaxLength(13);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Subtitle)
                    .HasMaxLength(500);

                entity.Property(e => e.Publisher)
                    .HasMaxLength(200);
            });

            // Configure BookCopy entity
            modelBuilder.Entity<BookCopy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BookId);
                entity.HasIndex(e => new { e.BookId, e.CopyNumber }).IsUnique();

                entity.Property(e => e.CopyNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Notes)
                    .HasMaxLength(100);

                entity.Property(e => e.Condition)
                    .HasDefaultValue("New");

                entity.HasOne(e => e.Book)
                    .WithMany(b => b.Copies)
                    .HasForeignKey(e => e.BookId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Author entity
            modelBuilder.Entity<Author>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.OpenLibraryKey)
                    .IsUnique()
                    .HasFilter("OpenLibraryKey IS NOT NULL");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.OpenLibraryKey)
                    .HasMaxLength(50);
            });

            // Configure BookAuthors many-to-many
            modelBuilder.Entity<Book>()
                .HasMany(b => b.Authors)
                .WithMany(a => a.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "BookAuthors",
                    j => j.HasOne<Author>().WithMany().HasForeignKey("AuthorId"),
                    j => j.HasOne<Book>().WithMany().HasForeignKey("BookId"),
                    j =>
                    {
                        j.HasKey("BookId", "AuthorId");
                        j.Property<int>("OrderIndex").HasDefaultValue(0);
                    });

            // Configure Borrower entity
            modelBuilder.Entity<Borrower>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Email)
                    .HasMaxLength(200);

                entity.Property(e => e.Phone)
                    .HasMaxLength(50);
            });

            // Configure Loan entity
            modelBuilder.Entity<Loan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BookId);
                entity.HasIndex(e => e.BookCopyId);
                entity.HasIndex(e => e.BorrowerId);
                entity.HasIndex(e => e.ReturnDate);

                entity.HasOne(e => e.Book)
                    .WithMany(b => b.Loans)
                    .HasForeignKey(e => e.BookId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.BookCopy)
                    .WithMany(bc => bc.Loans)
                    .HasForeignKey(e => e.BookCopyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Borrower)
                    .WithMany(b => b.Loans)
                    .HasForeignKey(e => e.BorrowerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Renewal entity
            modelBuilder.Entity<Renewal>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Loan)
                    .WithMany(r => r.Renewals)
                    .HasForeignKey(e => e.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Genre entity
            modelBuilder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // Configure BookGenres many-to-many
            modelBuilder.Entity<Book>()
                .HasMany(b => b.Genres)
                .WithMany(c => c.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "BookGenres",
                    j => j.HasOne<Genre>().WithMany().HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Book>().WithMany().HasForeignKey("BookId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasKey("BookId", "GenreId"));

            // Configure Tag entity
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // Configure BookTags entity many-to-many
            modelBuilder.Entity<Book>()
               .HasMany(b => b.Tags)
               .WithMany(c => c.Books)
               .UsingEntity<Dictionary<string, object>>(
                   "BookTags",
                   j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade),
                   j => j.HasOne<Book>().WithMany().HasForeignKey("BookId")
                        .OnDelete(DeleteBehavior.Cascade),
                   j => j.HasKey("BookId", "TagId"));

            // Configure Collection entity
            modelBuilder.Entity<Collection>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(200);
            });

            // Configure BookCollections entity many-to-many
            modelBuilder.Entity<Book>()
               .HasMany(b => b.Collections)
               .WithMany(c => c.Books)
               .UsingEntity<Dictionary<string, object>>(
                   "BookCollections",
                   j => j.HasOne<Collection>().WithMany().HasForeignKey("CollectionId")
                        .OnDelete(DeleteBehavior.Cascade),
                   j => j.HasOne<Book>().WithMany().HasForeignKey("BookId")
                        .OnDelete(DeleteBehavior.Cascade),
                   j => j.HasKey("BookId", "CollectionId"));

            // Seed default categories
            modelBuilder.Entity<Genre>().HasData(
                new Genre { Id = 1, Name = "Fantasy" },
                new Genre { Id = 2, Name = "Science Fiction" },
                new Genre { Id = 3, Name = "Reference" },
                new Genre { Id = 4, Name = "Language" }
            );
        }

        public override int SaveChanges()
        {
            NormalizeBookISBNs();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeBookISBNs();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void NormalizeBookISBNs()
        {
            var bookEntries = ChangeTracker.Entries<Book>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in bookEntries)
            {
                var book = entry.Entity;
                if (!string.IsNullOrWhiteSpace(book.ISBN))
                {
                    var cleanedISBN = ISBNValidator.CleanISBN(book.ISBN);
                    if (cleanedISBN.Length == 10)
                    {
                        book.ISBN = ISBNValidator.ConvertISBN10ToISBN13(cleanedISBN)!;
                    }
                }
            }
        }
    }
}