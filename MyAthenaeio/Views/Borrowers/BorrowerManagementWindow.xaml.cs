using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Borrowers
{
    public partial class BorrowerManagementWindow : Window
    {
        private readonly ObservableCollection<Borrower> _borrowers;
        private bool _changesMade = false;

        public BorrowerManagementWindow()
        {
            InitializeComponent();
            _borrowers = [];
            BorrowersDataGrid.ItemsSource = _borrowers;

            Loaded += async (s, e) => await SearchBorrowersAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await SearchBorrowersAsync();
        }

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SearchBorrowersAsync();
            }
        }

        private async void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await SearchBorrowersAsync();
            }
        }

        private async void OverdueFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await SearchBorrowersAsync();
            }
        }

        private async Task SearchBorrowersAsync()
        {
            try
            {
                // Get search term
                var searchTerm = SearchTextBox?.Text?.Trim();

                // Get all borrowers with loan data
                var borrowers = string.IsNullOrWhiteSpace(searchTerm)
                    ? await LibraryService.GetAllBorrowersAsync(BorrowerIncludeOptions.WithLoans)
                    : await LibraryService.SearchBorrowersAsync(searchTerm, BorrowerIncludeOptions.WithLoans);

                // Apply status filter
                var statusFilter = StatusFilterComboBox?.SelectedIndex ?? 0;
                if (statusFilter == 1) // Active Only
                {
                    borrowers = [.. borrowers.Where(b => b.IsActive)];
                }
                else if (statusFilter == 2) // Inactive Only
                {
                    borrowers = [.. borrowers.Where(b => !b.IsActive)];
                }

                // Apply overdue filter
                var overdueFilter = OverdueFilterComboBox?.SelectedIndex ?? 0;
                if (overdueFilter == 1) // With Overdue Books
                {
                    borrowers = [.. borrowers.Where(b => b.HasOverdueLoans)];
                }
                else if (overdueFilter == 2) // No Overdue Books
                {
                    borrowers = [.. borrowers.Where(b => !b.HasOverdueLoans)];
                }

                // Sort by name
                borrowers = [.. borrowers.OrderBy(b => b.Name)];

                // Update collection
                _borrowers.Clear();
                foreach (var borrower in borrowers)
                {
                    _borrowers.Add(borrower);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching borrowers: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Search error: {ex}");
            }
        }

        private void AddBorrower_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BorrowerAddDialog() { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _ = SearchBorrowersAsync();
                _changesMade = true;
            }
        }

        private void EditBorrower_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not Borrower borrower)
                return;

            var dialog = new BorrowerEditDialog(borrower.Id) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _ = SearchBorrowersAsync();
                _changesMade = true;
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not Borrower borrower)
                return;

            var detailsWindow = new BorrowerDetailWindow(borrower.Id) { Owner = this };

            if (detailsWindow.ShowDialog() == true)
            {
                _ = SearchBorrowersAsync();
                _changesMade = true;
            }
        }

        private void BorrowersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BorrowersDataGrid.SelectedItem is Borrower borrower)
            {
                var detailsWindow = new BorrowerDetailWindow(borrower.Id) { Owner = this };

                if (detailsWindow.ShowDialog() == true)
                {
                    _ = SearchBorrowersAsync();
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