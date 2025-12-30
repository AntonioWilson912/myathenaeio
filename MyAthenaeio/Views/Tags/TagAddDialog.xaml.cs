using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Tags
{
    public partial class TagAddDialog : Window
    {
        private readonly List<Tag> _existingBookTags;
        private List<Tag> _allTags;
        public Tag? SelectedTag { get; private set; }

        public TagAddDialog(List<Tag> existingBookTags)
        {
            InitializeComponent();
            _existingBookTags = existingBookTags;
            _allTags = new List<Tag>();

            Loaded += async (s, e) => await LoadTagsAsync();
            NameTextBox.TextChanged += (s, e) => UpdateAddButtonState();
        }

        private async Task LoadTagsAsync()
        {
            _allTags = await LibraryService.GetAllTagsAsync();
            _allTags.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            FilterTags();
        }

        private void FilterTags(string? searchTerm = null)
        {
            var filtered = _allTags.AsEnumerable();

            // Exclude tags already associated with the book
            filtered = filtered.Where(g => !_existingBookTags.Any(eb => eb.Id == g.Id));

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                filtered = filtered.Where(g => g.Name.ToLower().Contains(lowerSearch));
            }

            ExistingTagsListBox.ItemsSource = filtered.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTags(SearchTextBox.Text);
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
                var existingTag = _allTags.FirstOrDefault(g =>
                    g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existingTag != null)
                {
                    // Tag already exists - just select it and add to book
                    if (_existingBookTags.Any(g => g.Id == existingTag.Id))
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