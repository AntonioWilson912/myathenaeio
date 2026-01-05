using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Collections
{
    public partial class CollectionAddDialog : Window
    {
        private readonly int _bookId;
        private readonly ObservableCollection<Collection> _existingBookCollections;
        private readonly ObservableCollection<Collection> _allCollections;
        private readonly ObservableCollection<Collection> _filteredCollections;
        public Collection? SelectedCollection { get; private set; }

        public CollectionAddDialog(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            _existingBookCollections = [];
            _allCollections = [];
            _filteredCollections = [];

            ExistingCollectionsDataGrid.ItemsSource = _filteredCollections;

            Loaded += async (s, e) => await LoadCollectionsAsync();
            NameTextBox.TextChanged += (s, e) => UpdateAddButtonState();
        }

        private async Task LoadCollectionsAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Load book's existing collections
                var book = await LibraryService.GetBookByIdAsync(_bookId, BookIncludeOptions.WithGenres);
                if (book == null)
                {
                    MessageBox.Show("Book not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                _existingBookCollections.Clear();
                foreach (var collection in book.Collections)
                {
                    _existingBookCollections.Add(collection);
                }

                // Load all collections
                var allCollections = await LibraryService.GetAllCollectionsAsync();
                _allCollections.Clear();
                foreach (var collection in allCollections.OrderBy(g => g.Name))
                {
                    _allCollections.Add(collection);
                }

                FilterCollections();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading collections: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void FilterCollections(string? searchTerm = null)
        {
            var filtered = _allCollections.AsEnumerable();

            // Exclude collections already associated with the book
            filtered = filtered.Where(c => !_existingBookCollections.Any(eb => eb.Id == c.Id));

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                filtered = filtered.Where(c =>
                    c.Name.ToLower().Contains(lowerSearch) ||
                    (c.Description != null && c.Description.ToLower().Contains(lowerSearch)));
            }

            _filteredCollections.Clear();

            foreach (var collection in filtered)
            {
                _filteredCollections.Add(collection);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterCollections(SearchTextBox.Text);
        }

        private void ExistingCollectionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddButton.IsEnabled = ExistingCollectionsDataGrid.SelectedItem != null;
        }

        private void ExistingCollectionsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExistingCollectionsDataGrid.SelectedItem is Collection collection)
            {
                SelectCollection(collection);
            }
        }

        private void SelectCollection(Collection collection)
        {
            SelectedCollection = collection;
            DialogResult = true;
            Close();
        }

        private void UpdateAddButtonState()
        {
            AddButton.IsEnabled = !string.IsNullOrWhiteSpace(NameTextBox.Text);
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            // Check if we're on "Existing Collection" tab and one is selected
            if (ExistingCollectionsDataGrid.SelectedItem is Collection selectedCollection)
            {
                SelectCollection(selectedCollection);
                return;
            }

            var name = NameTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Collection name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Check if collection already exists (case-insensitive)
                var existingCollection = _allCollections.FirstOrDefault(c =>
                    c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existingCollection != null)
                {
                    // Collection already exists - just select it and add to book
                    if (_existingBookCollections.Any(c => c.Id == existingCollection.Id))
                    {
                        MessageBox.Show($"Collection '{existingCollection.Name}' is already associated with this book.",
                            "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    SelectCollection(existingCollection);
                    return;
                }

                // Create new collection with all fields
                var newCollection = await LibraryService.AddCollectionAsync(
                    name,
                    DescriptionTextBox.Text?.Trim(),
                    NotesTextBox.Text?.Trim()
                );


                SelectCollection(newCollection);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding collection: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}