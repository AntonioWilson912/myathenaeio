using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Views.BookCopies
{
    public partial class BookCopyDetailWindow : Window
    {
        private readonly int _bookCopyId;
        private BookCopy? _bookCopy;
        private bool _changesMade = false;

        public BookCopyDetailWindow(int bookCopyId)
        {
            InitializeComponent();
            _bookCopyId = bookCopyId;

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
                _bookCopy = await LibraryService.GetBookCopyByIdAsync(_bookCopyId, BookCopyIncludeOptions.Full);
                if (_bookCopy == null)
                {
                    MessageBox.Show("Book copy not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Book information
                BookTitleTextBlock.Text = _bookCopy.Book.Title;
                BookAuthorsTextBlock.Text = string.Join(", ", _bookCopy.Book.Authors.Select(a => a.Name));

                if (!string.IsNullOrEmpty(_bookCopy.Book.CoverImageUrl))
                {
                    try
                    {
                        BookCoverImage.Source = new BitmapImage(new Uri(_bookCopy.Book.CoverImageUrl));
                    }
                    catch { /* Cover failed to load */ }
                }

                // Copy information
                CopyNumberTextBlock.Text = _bookCopy.CopyNumber;
                AcquisitionDateTextBlock.Text = _bookCopy.AcquisitionDate.ToString("yyyy-MM-dd");
                ConditionTextBlock.Text = _bookCopy.Condition ?? "Not specified";

                StatusTextBlock.Text = _bookCopy.IsAvailable ? "Available" : "On Loan";
                StatusTextBlock.Foreground = _bookCopy.IsAvailable
                    ? Brushes.Green
                    : Brushes.OrangeRed;

                NotesTextBlock.Text = string.IsNullOrEmpty(_bookCopy.Notes)
                    ? "No notes"
                    : _bookCopy.Notes;

                // Loan history
                var loans = _bookCopy.Loans.OrderByDescending(l => l.CheckoutDate).ToList();
                LoansDataGrid.ItemsSource = loans;

                NoLoansText.Visibility = loans.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Button states
                var activeLoan = loans.FirstOrDefault(l => l.ReturnDate == null);
                CheckoutButton.IsEnabled = _bookCopy.IsAvailable;
                ReturnButton.IsEnabled = !_bookCopy.IsAvailable && activeLoan != null;

                // Store active loan for return button
                ReturnButton.Tag = activeLoan;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var editDialog = new BookCopyEditDialog(_bookCopy!.Id) { Owner = this };

            if (editDialog.ShowDialog() == true)
            {
                _changesMade = true;
                _ = LoadDataAsync();
            }
        }

        private void ViewBook_Click(object sender, RoutedEventArgs e)
        {
            var bookDetailWindow = new Books.BookDetailWindow(_bookCopy!.Book.Id) { Owner = this };

            if (bookDetailWindow.ShowDialog() == true)
            {
                _changesMade = true;
                _ = LoadDataAsync();
            }
        }

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            // Open checkout dialog with this specific copy
            var checkoutDialog = new Loans.CheckoutBookDialog(_bookCopyId) { Owner = this };

            if (checkoutDialog.ShowDialog() == true)
            {
                _changesMade = true;
                _ = LoadDataAsync();
            }
        }

        private async void Return_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Loan activeLoan)
                return;

            var result = MessageBox.Show(
                $"Return this copy from {activeLoan.Borrower.Name}?\n\n" +
                $"Book: {_bookCopy?.Book.Title}\n" +
                $"Copy: #{_bookCopy?.CopyNumber}\n" +
                $"Checked out: {activeLoan.CheckoutDate:yyyy-MM-dd}\n" +
                $"Due: {activeLoan.DueDate:yyyy-MM-dd}",
                "Confirm Return",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.ReturnBookAsync(activeLoan.Id);

                _changesMade = true;

                MessageBox.Show(
                    $"Book successfully returned from {activeLoan.Borrower.Name}.",
                    "Return Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error returning book: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoansDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LoansDataGrid.SelectedItem is Loan loan)
            {
                var loanDetailWindow = new Loans.LoanDetailWindow(loan.Id) { Owner = this };

                if (loanDetailWindow.ShowDialog() == true)
                {
                    _changesMade = true;
                    _ = LoadDataAsync();
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}