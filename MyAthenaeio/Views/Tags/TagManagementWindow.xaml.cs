using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Tags
{
    /// <summary>
    /// Interaction logic for TagManagementWindow.xaml
    /// </summary>
    public partial class TagManagementWindow : Window
    {
        private readonly ObservableCollection<Tag> _tags;
        private bool _changesMade = false;

        public TagManagementWindow()
        {
            InitializeComponent();
            _tags = new ObservableCollection<Tag>();
            TagsDataGrid.ItemsSource = _tags;

            Loaded += async (s, e) => await SearchTagsAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task SearchTagsAsync()
        {
            try
            {
                // Get search term
                var searchTerm = SearchTextBox?.Text?.Trim();

                // Get all tags or search
                var tags = string.IsNullOrWhiteSpace(searchTerm)
                    ? await LibraryService.GetAllTagsAsync()
                    : await LibraryService.SearchTagsAsync(searchTerm);

                // Load book counts for all tags
                foreach (var tag in tags)
                {
                    tag.BookCount = await LibraryService.GetBookCountWithTagAsync(tag.Id);
                }

                // Apply book filter
                var bookFilter = BookFilterComboBox?.SelectedIndex ?? 0;
                if (bookFilter == 1) // With Books
                {
                    tags = [.. tags.Where(g => g.BookCount > 0)];
                }
                else if (bookFilter == 2) // Without Books
                {
                    tags = [.. tags.Where(g => g.BookCount == 0)];
                }

                // Sort by name
                tags = [.. tags.OrderBy(g => g.Name)];

                // Update collection
                _tags.Clear();
                foreach (var tag in tags)
                {
                    _tags.Add(tag);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching tags: {ex.Message}",
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
                _ = SearchTagsAsync();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SearchTagsAsync();
        }

        private void BookFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                _ = SearchTagsAsync();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchTagsAsync();
        }

        private async void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TagCreateDialog { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _changesMade = true;
                await SearchTagsAsync();
            }
        }

        private async void TagsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TagsDataGrid.SelectedItem is Tag tag)
            {
                await OpenTagDetails(tag);
            }
        }

        private async void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tag tag)
            {
                await OpenTagDetails(tag);
            }
        }

        private async Task OpenTagDetails(Tag tag)
        {
            var detailWindow = new TagDetailWindow(tag.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                _changesMade = true;
                await SearchTagsAsync();
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tag tag)
            {
                var dialog = new TagEditDialog(tag.Id) { Owner = this };

                if (dialog.ShowDialog() == true)
                {
                    _changesMade = true;
                    await SearchTagsAsync();
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Tag tag)
                return;

            // Check if tag has books
            if (tag.BookCount > 0)
            {
                MessageBox.Show(
                    $"Cannot delete tag '{tag.Name}' because it is associated with {tag.BookCount} book(s).\n\n" +
                    "Remove the tag from all books before deleting it.",
                    "Cannot Delete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the tag '{tag.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.DeleteTagAsync(tag.Id);

                MessageBox.Show($"Tag '{tag.Name}' deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _changesMade = true;
                await SearchTagsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting tag: {ex.Message}",
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