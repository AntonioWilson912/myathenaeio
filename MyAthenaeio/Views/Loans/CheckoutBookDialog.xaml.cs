using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Loans
{
    public partial class CheckoutBookDialog : Window
    {
        private readonly int _bookCopyId;
        private BookCopy? _bookCopy;
        private readonly SettingsService _settingsService;
        private readonly ObservableCollection<Borrower> _borrowers;

        public CheckoutBookDialog(int bookCopyId)
        {
            InitializeComponent();
            _bookCopyId = bookCopyId;
            _settingsService = new SettingsService();
            _borrowers = [];

            BorrowerComboBox.ItemsSource = _borrowers;

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Load the book copy
                _bookCopy = await LibraryService.GetBookCopyByIdAsync(_bookCopyId);
                if (_bookCopy == null)
                {
                    MessageBox.Show("Book copy not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Check if copy is available
                if (!_bookCopy.IsAvailable)
                {
                    MessageBox.Show("This copy is currently on loan and cannot be checked out.",
                        "Unavailable",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                // Display book information
                BookTitleTextBlock.Text = _bookCopy.Book.Title;
                if (_bookCopy.Book.Authors.Count != 0)
                {
                    BookTitleTextBlock.Text += $" by {string.Join(", ", _bookCopy.Book.Authors.Select(a => a.Name))}";
                }

                CopyInfoTextBlock.Text = $"Copy #{_bookCopy.CopyNumber}";
                if (!string.IsNullOrEmpty(_bookCopy.Condition))
                {
                    CopyInfoTextBlock.Text += $" • Condition: {_bookCopy.Condition}";
                }

                // Set default dates
                CheckoutDatePicker.SelectedDate = DateTime.Today;
                DueDatePicker.SelectedDate = DateTime.Today.AddDays(_settingsService.Settings.DefaultLoanDays);

                // Load borrowers
                await LoadBorrowersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadBorrowersAsync()
        {
            try
            {
                // Load active borrowers with loan data for HasOverdueLoans property
                var borrowers = await LibraryService.GetActiveBorrowersAsync(BorrowerIncludeOptions.WithLoans);

                _borrowers.Clear();
                foreach (var borrower in borrowers.OrderBy(b => b.Name))
                {
                    _borrowers.Add(borrower);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading borrowers: {ex.Message}");
            }
        }

        private void BorrowerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BorrowerComboBox.SelectedItem is Borrower borrower)
            {
                BorrowerEmailTextBlock.Text = borrower.Email ?? "No email on file";

                // Check if borrower already has this book
                _ = CheckBorrowerEligibilityAsync(borrower);
            }
            else
            {
                BorrowerEmailTextBlock.Text = string.Empty;
            }
        }

        private async Task CheckBorrowerEligibilityAsync(Borrower borrower)
        {
            // Window would have closed if _bookCopy was null
            try
            {
                var borrowerLoans = await LibraryService.GetActiveLoansByBorrowerAsync(borrower.Id);
                var hasThisBook = borrowerLoans.Any(l => l.BookId == _bookCopy!.BookId);

                if (hasThisBook)
                {
                    MessageBox.Show(
                        $"{borrower.Name} already has a copy of this book checked out.\n\n" +
                        "Please return the other copy before checking out another.",
                        "Duplicate Loan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking borrower eligibility: {ex.Message}");
            }
        }

        private async void Checkout_Click(object sender, RoutedEventArgs e)
        {
            // Window would have closed if _bookCopy was null
            try
            {
                // Validate borrower
                if (BorrowerComboBox.SelectedItem is not Borrower selectedBorrower)
                {
                    MessageBox.Show("Please select a borrower.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!selectedBorrower.IsActive)
                {
                    MessageBox.Show("This borrower is inactive and cannot check out books.",
                        "Inactive Borrower",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate dates
                if (!CheckoutDatePicker.SelectedDate.HasValue || !DueDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Checkout date and due date are required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DueDatePicker.SelectedDate < CheckoutDatePicker.SelectedDate)
                {
                    MessageBox.Show("Due date cannot be before checkout date.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate loan
                var borrowerLoans = await LibraryService.GetActiveLoansByBorrowerAsync(selectedBorrower.Id);
                if (borrowerLoans.Any(l => l.BookId == _bookCopy!.BookId))
                {
                    MessageBox.Show(
                        $"{selectedBorrower.Name} already has a copy of this book checked out.\n\n" +
                        "Cannot check out multiple copies of the same book.",
                        "Duplicate Loan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                // Create the loan
                var loan = await LibraryService.CheckoutBookAsync(
                    _bookCopyId,
                    selectedBorrower.Id,
                    _settingsService.Settings.MaxRenewals,
                    _settingsService.Settings.DefaultLoanDays
                );

                // Update checkout and due dates if different from defaults
                var needsDateUpdate = CheckoutDatePicker.SelectedDate != DateTime.Today ||
                                     DueDatePicker.SelectedDate != DateTime.Today.AddDays(_settingsService.Settings.DefaultLoanDays);
                var hasNotes = !string.IsNullOrWhiteSpace(NotesTextBox.Text);

                if (needsDateUpdate || hasNotes)
                {
                    var loanToUpdate = await LibraryService.GetLoanByIdAsync(loan.Id);
                    if (loanToUpdate != null)
                    {
                        if (needsDateUpdate)
                        {
                            loanToUpdate.CheckoutDate = CheckoutDatePicker.SelectedDate!.Value;
                            loanToUpdate.DueDate = DueDatePicker.SelectedDate!.Value;
                        }

                        if (hasNotes)
                        {
                            loanToUpdate.Notes = NotesTextBox.Text.Trim();
                        }

                        await LibraryService.UpdateLoanAsync(loanToUpdate);
                    }
                }

                MessageBox.Show(
                    $"Book successfully checked out to {selectedBorrower.Name}!\n\n" +
                    $"Due back: {DueDatePicker.SelectedDate.Value:yyyy-MM-dd}\n" +
                    $"Renewals allowed: {_settingsService.Settings.MaxRenewals}",
                    "Checkout Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking out book: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}