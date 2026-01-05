using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using MyAthenaeio.Views.Loans;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace MyAthenaeio.Views.Borrowers
{
    /// <summary>
    /// Interaction logic for BorrowerDetailWindow.xaml
    /// </summary>
    public partial class BorrowerDetailWindow : Window
    {
        private readonly int _borrowerId;
        private Borrower? _borrower;
        private bool _changesMade = false;

        public BorrowerDetailWindow(int borrowerId)
        {
            InitializeComponent();
            _borrowerId = borrowerId;

            Loaded += async (s, e) => await LoadBorrowerDetailsAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        public async void RefreshData()
        {
            await LoadBorrowerDetailsAsync();
        }

        private async Task LoadBorrowerDetailsAsync()
        {
            try
            {
                _borrower = await LibraryService.GetBorrowerByIdAsync(_borrowerId, BorrowerIncludeOptions.WithLoans);
                if (_borrower == null)
                {
                    MessageBox.Show("Borrower not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Update UI
                NameTextBlock.Text = _borrower.Name;
                EmailTextBlock.Text = _borrower.Email ?? "Not provided";
                PhoneTextBlock.Text = _borrower.Phone ?? "Not provided";
                RegisteredDateTextBlock.Text = _borrower.DateAdded.ToString("yyyy-MM-dd");
                StatusTextBlock.Text = _borrower.IsActive ? "Active" : "Inactive";
                NotesTextBlock.Text = _borrower.Notes ?? "No notes";

                // Load loan history
                var loanHistory = await LibraryService.GetLoansByBorrowerAsync(_borrowerId);
                var activeLoans = loanHistory.Where(l => !l.IsReturned).ToList();
                var overdueCount = activeLoans.Count(l => l.IsOverdue);

                ActiveLoansTextBlock.Text = activeLoans.Count.ToString();
                TotalLoansTextBlock.Text = loanHistory.Count.ToString();
                OverdueLoansTextBlock.Text = overdueCount.ToString();

                if (overdueCount > 0)
                {
                    OverdueLoansTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    OverdueLoansTextBlock.FontWeight = FontWeights.Bold;
                }

                ActiveLoansDataGrid.ItemsSource = activeLoans;
                LoanHistoryDataGrid.ItemsSource = loanHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading borrower details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BorrowerEditDialog(_borrowerId) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                RefreshData();
                _changesMade = true;
            }
        }

        private void LoanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as DataGrid)?.SelectedItem is Loan loan)
            {
                var detailsWindow = new LoanDetailWindow(loan.Id) { Owner = this };

                if (detailsWindow.ShowDialog() == true)
                {
                    RefreshData();
                    _changesMade = true;
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (!_changesMade)
                DialogResult = false;
            Close();
        }
    }
}
