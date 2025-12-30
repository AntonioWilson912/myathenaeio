using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Collections
{
    public partial class CollectionAddDialog : Window
    {
        private readonly List<Collection> _existingBookCollections;
        private List<Collection> _allCollections;
        public Collection? SelectedCollection { get; private set; }

        public CollectionAddDialog(List<Collection> existingBookCollections)
        {
            InitializeComponent();
            _existingBookCollections = existingBookCollections;
            _allCollections = new List<Collection>();

            Loaded += async (s, e) => await LoadCollectionsAsync();
            NameTextBox.TextChanged += (s, e) => UpdateAddButtonState();
        }

        private async Task LoadCollectionsAsync()
        {
            _allCollections = await LibraryService.GetAllCollectionsAsync();
            _allCollections.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            FilterCollections();
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

            ExistingCollectionsDataGrid.ItemsSource = filtered.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterCollections(SearchTextBox.Text);
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
            DialogResult = false;
            Close();
        }
    }
}