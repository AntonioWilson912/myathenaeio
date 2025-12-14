using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAthenaeio.Models
{
    public class BookAvailability
    {
        public bool BookExists { get; set; }
        public int TotalCopies { get; set; }
        public int OnLoan { get; set; }
        public int Available { get; set; }
        public bool IsAvailable => Available > 0;
    }
}
