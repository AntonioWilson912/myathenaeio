using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Utils
{
    public static class LoanExtension
    {
        public static DateTime GetEffectiveDueDate(this Loan loan)
        {
            if (loan.Renewals.Count > 0)
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
