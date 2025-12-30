using MyAthenaeio.Models.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyAthenaeio.Models.Entities
{
    public class BookCopy
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CopyNumber { get; set; } = string.Empty;
        public DateTime AcquisitionDate { get; set; } = DateTime.Now;

        public bool IsAvailable { get; set; } = true;

        [MaxLength(100)]
        public string? Notes { get; set; }

        // Navigation property
        public virtual Book Book { get; set; } = null!;
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}