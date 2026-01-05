using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Loans
{
    public partial class LoanManagementWindow : Window, INotifyPropertyChanged
    {
        private readonly int _bookId;
        private readonly SettingsService _settingsService;
        private readonly ObservableCollection<Loan> _activeLoans;
        private readonly ObservableCollection<Loan> _loanHistory;
        private readonly ObservableCollection<Borrower> _borrowers;
        private readonly ObservableCollection<BookCopy> _copies;
        private bool _changesMade = false;

        public bool HasOverdueLoans => _activeLoans?.Any(l => l.IsOverdue) ?? false;

        public LoanManagementWindow(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            _settingsService = new SettingsService();
            _activeLoans = [];
            _loanHistory = [];
            _borrowers = [];
            _copies = [];

            // Set ItemsSource once in constructor
            BorrowerComboBox.ItemsSource = _borrowers;
            CopyComboBox.ItemsSource = _copies;
            ActiveLoansDataGrid.ItemsSource = _activeLoans;
            LoanHistoryDataGrid.ItemsSource = _loanHistory;

            // DataContext needed for HasOverdueLoans binding
            DataContext = this;

            Loaded += async (s, e) => await LoadDataAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Load book
                var book = await LibraryService.GetBookByIdAsync(_bookId, BookIncludeOptions.Full);
                if (book == null)
                {
                    MessageBox.Show("Book not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                BookInfoTextBlock.Text = $"{book.Title} by {book.AuthorsDisplay}";

                // Load copies
                _copies.Clear();
                foreach (var copy in book.Copies)
                {
                    _copies.Add(copy);
                }

                // Select first available copy
                CopyComboBox.SelectedIndex = book.Copies.Any(c => c.IsAvailable)
                    ? book.Copies.ToList().FindIndex(c => c.IsAvailable)
                    : -1;

                // Set default dates
                LoanDatePicker.SelectedDate = DateTime.Today;
                DueDatePicker.SelectedDate = DateTime.Today.AddDays(_settingsService.Settings.DefaultLoanDays);

                await LoadBorrowersAsync();
                await LoadLoansAsync();

                // Update overdue warning
                UpdateOverdueWarning();
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
                var borrowers = await LibraryService.GetActiveBorrowersAsync(BorrowerIncludeOptions.WithLoans);

                _borrowers.Clear();
                foreach (var borrower in borrowers.OrderBy(b => b.Name))
                {
                    _borrowers.Add(borrower);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading borrowers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BorrowerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BorrowerComboBox.SelectedItem is Borrower borrower)
            {
                BorrowerEmailTextBlock.Text = borrower.Email ?? "No email provided";
                _ = CheckBorrowerEligibilityAsync(borrower);
            }
            else
            {
                BorrowerEmailTextBlock.Text = string.Empty;
            }
        }

        private async Task CheckBorrowerEligibilityAsync(Borrower borrower)
        {
            try
            {
                var borrowerLoans = await LibraryService.GetActiveLoansByBorrowerAsync(borrower.Id);
                var hasThisBook = borrowerLoans.Any(l => l.BookId == _bookId);

                if (hasThisBook)
                {
                    MessageBox.Show(
                        $"{borrower.Name} already has a copy of this book checked out.",
                        "Duplicate Loan Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking eligibility: {ex.Message}");
            }
        }

        private async Task LoadLoansAsync()
        {
            try
            {
                var allLoans = await LibraryService.GetLoansByBookAsync(_bookId, LoanIncludeOptions.Default);

                var activeLoans = allLoans.Where(l => !l.IsReturned).ToList();

                _activeLoans.Clear();
                foreach (var loan in activeLoans)
                {
                    _activeLoans.Add(loan);
                }

                _loanHistory.Clear();
                foreach (var loan in allLoans)
                {
                    _loanHistory.Add(loan);
                }

                OnPropertyChanged(nameof(HasOverdueLoans));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading loans: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateOverdueWarning()
        {
            var overdueCount = _activeLoans.Count(l => l.IsOverdue);
            OverdueWarningTextBlock.Text = overdueCount > 0
                ? $"⚠ {overdueCount} loan{(overdueCount == 1 ? " is" : "s are")} overdue!"
                : string.Empty;
        }

        private async void Checkout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate copy
                if (CopyComboBox.SelectedItem is not BookCopy selectedCopy)
                {
                    MessageBox.Show("Please select a copy to loan.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!selectedCopy.IsAvailable)
                {
                    MessageBox.Show("This copy is not available for loan.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate borrower
                if (BorrowerComboBox.SelectedItem is not Borrower selectedBorrower)
                {
                    MessageBox.Show("Please select a borrower.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!selectedBorrower.IsActive)
                {
                    MessageBox.Show("This borrower is inactive.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate loan
                var borrowerLoans = await LibraryService.GetActiveLoansByBorrowerAsync(selectedBorrower.Id);
                if (borrowerLoans.Any(l => l.BookId == _bookId))
                {
                    MessageBox.Show(
                        $"{selectedBorrower.Name} already has a copy of this book checked out.",
                        "Duplicate Loan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Validate dates
                if (!LoanDatePicker.SelectedDate.HasValue || !DueDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Loan date and due date are required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                // Check out the book
                var loan = await LibraryService.CheckoutBookAsync(
                    selectedCopy.Id,
                    selectedBorrower.Id,
                    _settingsService.Settings.MaxRenewals,
                    _settingsService.Settings.DefaultLoanDays
                );

                // Update notes if provided
                if (!string.IsNullOrWhiteSpace(LoanNotesTextBox.Text))
                {
                    loan.Notes = LoanNotesTextBox.Text.Trim();
                    await LibraryService.UpdateLoanAsync(loan);
                }

                MessageBox.Show(
                    $"Book successfully checked out to {selectedBorrower.Name}.\n\n" +
                    $"Due back: {loan.DueDate:yyyy-MM-dd}\n" +
                    $"Renewals allowed: {loan.MaxRenewalsAllowed}",
                    "Checkout Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _changesMade = true;
                ClearForm();
                await LoadDataAsync();

                // Switch to Active Loans tab
                if (CheckoutTab.Parent is TabControl tabControl)
                {
                    tabControl.SelectedIndex = 1;
                }
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

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            BorrowerComboBox.SelectedIndex = -1;
            BorrowerEmailTextBlock.Text = string.Empty;
            LoanNotesTextBox.Clear();
            LoanDatePicker.SelectedDate = DateTime.Today;
            DueDatePicker.SelectedDate = DateTime.Today.AddDays(_settingsService.Settings.DefaultLoanDays);

            // Select first available copy
            if (_copies.Count > 0)
            {
                var firstAvailable = _copies.FirstOrDefault(c => c.IsAvailable);
                CopyComboBox.SelectedItem = firstAvailable;
            }
        }

        private async void Return_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not Loan loan)
                return;

            var result = MessageBox.Show(
                $"Return book from {loan.Borrower.Name}?",
                "Confirm Return",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.ReturnBookAsync(loan.Id);

                _changesMade = true;

                MessageBox.Show("Book returned successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error returning book: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Renew_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not Loan loan)
                return;

            if (loan.RenewalCount >= loan.MaxRenewalsAllowed)
            {
                MessageBox.Show(
                    $"Maximum renewals ({loan.MaxRenewalsAllowed}) reached for this loan.",
                    "Cannot Renew",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var renewal = await LibraryService.RenewLoanAsync(loan.Id);

                _changesMade = true;

                MessageBox.Show(
                    $"Loan renewed successfully!\n\n" +
                    $"New due date: {renewal.NewDueDate:yyyy-MM-dd}\n" +
                    $"Renewals remaining: {loan.MaxRenewalsAllowed - loan.RenewalCount - 1}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadLoansAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renewing loan: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewLoanDetails_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not Loan loan)
                return;

            OpenLoanDetails(loan.Id);
        }

        private void ActiveLoansDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ActiveLoansDataGrid.SelectedItem is Loan loan)
            {
                OpenLoanDetails(loan.Id);
            }
        }

        private void OpenLoanDetails(int loanId)
        {
            var loanDetailWindow = new LoanDetailWindow(loanId) { Owner = this };

            if (loanDetailWindow.ShowDialog() == true)
            {
                _changesMade = true;
                _ = LoadLoansAsync();
            }
        }

        private void ManageBorrowers_Click(object sender, RoutedEventArgs e)
        {
            var borrowerWindow = new Borrowers.BorrowerManagementWindow { Owner = this };

            if (borrowerWindow.ShowDialog() == true)
            {
                _ = LoadBorrowersAsync();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}