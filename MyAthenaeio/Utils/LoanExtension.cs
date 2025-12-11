using MyAthenaeio.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAthenaeio.Utils
{
    public static class LoanExtension
    {
        public static DateTime GetEffectiveDueDate(this Loan loan)
        {
            if (loan.Renewals.Count == 0)
            {
                return loan.Renewals
                    .OrderByDescending(r => r.RenewalDate)
                    .First()
                    .NewDueDate;
            }

            return loan.DueDate;
        }
    }
}
