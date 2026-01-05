using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Tags
{
    /// <summary>
    /// Interaction logic for TagAddDialog.xaml
    /// </summary>
    public partial class TagAddDialog : Window
    {
        private readonly int _bookId;
        private readonly ObservableCollection<Tag> _existingBookTags;
        private readonly ObservableCollection<Tag> _allTags;
        private readonly ObservableCollection<Tag> _filteredTags;
        public Tag? SelectedTag { get; private set; }

        public TagAddDialog(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            _existingBookTags = [];
            _allTags = [];
            _filteredTags = [];

            ExistingTagsListBox.ItemsSource = _filteredTags;

            Loaded += async (s, e) => await LoadDataAsync();
            NameTextBox.TextChanged += (s, e) => UpdateAddButtonState();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Load book's existing tags
                var book = await LibraryService.GetBookByIdAsync(_bookId, BookIncludeOptions.WithTags);
                if (book == null)
                {
                    MessageBox.Show("Book not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                _existingBookTags.Clear();
                foreach (var tag in book.Tags)
                {
                    _existingBookTags.Add(tag);
                }

                // Load all tags
                var allTags = await LibraryService.GetAllTagsAsync();
                _allTags.Clear();
                foreach (var tag in allTags.OrderBy(t => t.Name))
                {
                    _allTags.Add(tag);
                }

                FilterTags();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tags: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void FilterTags(string? searchTerm = null)
        {
            var filtered = _allTags.AsEnumerable();

            // Exclude tags already associated with the book
            filtered = filtered.Where(t => !_existingBookTags.Any(eb => eb.Id == t.Id));

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                filtered = filtered.Where(t => t.Name.ToLower().Contains(lowerSearch));
            }

            _filteredTags.Clear();

            foreach (var tag in filtered)
            {
                _filteredTags.Add(tag);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTags(SearchTextBox.Text);
        }

        private void ExistingTagsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddButton.IsEnabled = ExistingTagsListBox.SelectedItem != null;
        }

        private void ExistingTagsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExistingTagsListBox.SelectedItem is Tag tag)
            {
                SelectTag(tag);
            }
        }

        private void SelectTag(Tag tag)
        {
            SelectedTag = tag;
            DialogResult = true;
            Close();
        }

        private void UpdateAddButtonState()
        {
            AddButton.IsEnabled = !string.IsNullOrWhiteSpace(NameTextBox.Text);
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            // Check if in "Existing Tags" tab and one is selected
            if (ExistingTagsListBox.SelectedItem is Tag selectedTag)
            {
                SelectTag(selectedTag);
                return;
            }

            var name = NameTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Tag name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Check if tag already exists (case-insensitive)
                var existingTag = _allTags.FirstOrDefault(t =>
                    t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existingTag != null)
                {
                    // Tag already exists - check if already on this book
                    if (_existingBookTags.Any(t => t.Id == existingTag.Id))
                    {
                        MessageBox.Show($"Tag '{existingTag.Name}' is already associated with this book.",
                            "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    SelectTag(existingTag);
                    return;
                }

                // Create new tag
                var newTag = await LibraryService.AddTagAsync(name);
                SelectTag(newTag);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding tag: {ex.Message}",
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