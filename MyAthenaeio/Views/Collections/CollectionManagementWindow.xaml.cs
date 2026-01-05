using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Collections
{
    /// <summary>
    /// Interaction logic for CollectionManagementWindow.xaml
    /// </summary>
    public partial class CollectionManagementWindow : Window
    {
        private readonly ObservableCollection<Collection> _collections;
        private bool _changesMade = false;

        public CollectionManagementWindow()
        {
            InitializeComponent();
            _collections = [];
            CollectionsDataGrid.ItemsSource = _collections;

            Loaded += async (s, e) => await SearchCollectionsAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task SearchCollectionsAsync()
        {
            try
            {
                // Get search term
                var searchTerm = SearchTextBox?.Text?.Trim();

                // Get all collections or search
                var collections = string.IsNullOrWhiteSpace(searchTerm)
                    ? await LibraryService.GetAllCollectionsAsync()
                    : await LibraryService.SearchCollectionsAsync(searchTerm);

                // Load book counts for all collections
                foreach (var collection in collections)
                {
                    collection.BookCount = await LibraryService.GetBookCountInCollectionAsync(collection.Id);
                }

                // Apply book filter
                var bookFilter = BookFilterComboBox?.SelectedIndex ?? 0;
                if (bookFilter == 1) // With Books
                {
                    collections = [.. collections.Where(g => g.BookCount > 0)];
                }
                else if (bookFilter == 2) // Without Books
                {
                    collections = [.. collections.Where(g => g.BookCount == 0)];
                }

                // Sort by name
                collections = [.. collections.OrderBy(g => g.Name)];

                // Update collection
                _collections.Clear();
                foreach (var collection in collections)
                {
                    _collections.Add(collection);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching collections: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = SearchCollectionsAsync();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SearchCollectionsAsync();
        }

        private void BookFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                _ = SearchCollectionsAsync();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchCollectionsAsync();
        }

        private async void AddCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CollectionCreateDialog { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _changesMade = true;
                await SearchCollectionsAsync();
            }
        }

        private async void CollectionsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CollectionsDataGrid.SelectedItem is Collection collection)
            {
                await OpenCollectionDetails(collection);
            }
        }

        private async void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Collection collection)
            {
                await OpenCollectionDetails(collection);
            }
        }

        private async Task OpenCollectionDetails(Collection collection)
        {
            var detailWindow = new CollectionDetailWindow(collection.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                _changesMade = true;
                await SearchCollectionsAsync();
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Collection collection)
            {
                var dialog = new CollectionEditDialog(collection.Id) { Owner = this };

                if (dialog.ShowDialog() == true)
                {
                    _changesMade = true;
                    await SearchCollectionsAsync();
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Collection collection)
                return;

            // Check if collection has books
            if (collection.BookCount > 0)
            {
                MessageBox.Show(
                    $"Cannot delete collection '{collection.Name}' because it is associated with {collection.BookCount} book(s).\n\n" +
                    "Remove the collection from all books before deleting it.",
                    "Cannot Delete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the collection '{collection.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.DeleteCollectionAsync(collection.Id);

                MessageBox.Show($"Collection '{collection.Name}' deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _changesMade = true;
                await SearchCollectionsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting collection: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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