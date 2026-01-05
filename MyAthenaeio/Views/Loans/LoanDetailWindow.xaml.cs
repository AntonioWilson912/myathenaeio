using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using System.Windows;
using System.Windows.Media;

namespace MyAthenaeio.Views.Loans
{
    public partial class LoanDetailWindow : Window
    {
        private readonly int _loanId;
        private bool _changesMade = false;

        public LoanDetailWindow(int loanId)
        {
            InitializeComponent();
            _loanId = loanId;

            Loaded += async (s, e) => await LoadLoanDetailsAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadLoanDetailsAsync()
        {
            try
            {
                var loan = await LibraryService.GetLoanByIdAsync(_loanId, LoanIncludeOptions.Default);
                if (loan == null)
                {
                    MessageBox.Show("Loan not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Display loan information
                BookTextBlock.Text = $"{loan.BookCopy.Book.Title} by {loan.BookCopy.Book.AuthorsDisplay}";
                CopyTextBlock.Text = loan.BookCopy.CopyNumber;

                BorrowerNameTextBlock.Text = loan.Borrower.Name;
                BorrowerContactTextBlock.Text = loan.Borrower.Email ?? loan.Borrower.Phone ?? "No contact info";

                LoanDateTextBlock.Text = loan.CheckoutDate.ToString("yyyy-MM-dd");
                DueDateTextBlock.Text = loan.EffectiveDueDate.ToString("yyyy-MM-dd");
                ReturnDateTextBlock.Text = loan.ReturnDate?.ToString("yyyy-MM-dd") ?? "Not returned";
                RenewalsTextBlock.Text = $"{loan.RenewalCount} / {loan.MaxRenewalsAllowed} (Remaining: {loan.RenewalsRemaining})";
                NotesTextBox.Text = loan.Notes;

                // Update status
                UpdateStatusDisplay(loan);

                // Display renewal history
                var renewals = loan.Renewals.OrderByDescending(r => r.RenewalDate).ToList();
                if (renewals.Count > 0)
                {
                    RenewalHistoryDataGrid.ItemsSource = renewals;
                    RenewalHistoryGroupBox.Visibility = Visibility.Visible;
                }
                else
                {
                    RenewalHistoryGroupBox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading loan details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void UpdateStatusDisplay(Loan loan)
        {
            if (loan.ReturnDate != null)
            {
                StatusTextBlock.Text = "RETURNED";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                ReturnButton.IsEnabled = false;
                RenewButton.IsEnabled = false;
            }
            else if (loan.IsOverdue)
            {
                var daysOverdue = (DateTime.Now.Date - loan.GetEffectiveDueDate().Date).Days;
                StatusTextBlock.Text = $"OVERDUE ({daysOverdue} day{(daysOverdue == 1 ? "" : "s")})";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                ReturnButton.IsEnabled = true;
                RenewButton.IsEnabled = loan.RenewalsRemaining > 0;
            }
            else
            {
                var daysRemaining = (loan.GetEffectiveDueDate().Date - DateTime.Now.Date).Days;
                StatusTextBlock.Text = $"ACTIVE ({daysRemaining} day{(daysRemaining == 1 ? "" : "s")} remaining)";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.DarkGoldenrod);
                ReturnButton.IsEnabled = true;
                RenewButton.IsEnabled = loan.RenewalsRemaining > 0;
            }
        }

        private async void Return_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Return this book?",
                "Confirm Return",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.ReturnBookAsync(_loanId);

                MessageBox.Show("Book returned successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _changesMade = true;
                await LoadLoanDetailsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error returning book: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Renew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var renewal = await LibraryService.RenewLoanAsync(_loanId);

                _changesMade = true;

                MessageBox.Show(
                    $"Loan renewed successfully!\n\n" +
                    $"New due date: {renewal.NewDueDate:yyyy-MM-dd}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadLoanDetailsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renewing loan: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveNotes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loanToUpdate = await LibraryService.GetLoanByIdAsync(_loanId);
                if (loanToUpdate == null)
                {
                    MessageBox.Show("Loan not found.", "Error");
                    Close();
                    return;
                }

                loanToUpdate.Notes = NotesTextBox.Text?.Trim();
                await LibraryService.UpdateLoanAsync(loanToUpdate);

                _changesMade = true;

                MessageBox.Show("Notes saved successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving notes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}