using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models.Entities
{
    public class Book
    {
        public int Id {  get; set; }

        [Required]
        [MaxLength(13)]
        public string ISBN { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Subtitle { get; set; }

        [MaxLength(200)]
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public string? Description { get; set; }
        public DateTime? DateAdded { get; set; }
        public int Copies { get; set; }
        public string? Notes { get; set; }
        public string? CoverImageUrl { get; set; }
        public ICollection<Author> Authors { get; set; } = [];
        public ICollection<Loan> Loans { get; set; } = [];
        public ICollection<Genre> Genres { get; set; } = [];
        public ICollection<Tag> Tags { get; set; } = [];
        public ICollection<Collection> Collections { get; set; } = [];
    }
}