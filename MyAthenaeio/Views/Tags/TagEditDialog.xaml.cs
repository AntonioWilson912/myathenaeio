using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MyAthenaeio.Views.Tags
{
    /// <summary>
    /// Interaction logic for TagEditDialog.xaml
    /// </summary>
    public partial class TagEditDialog : Window
    {
        private readonly int _tagId;
        private ObservableCollection<Book> _tagBooks;
        private bool _changesMade = false;

        public TagEditDialog(int tagId)
        {
            InitializeComponent();
            _tagId = tagId;
            _tagBooks = [];

            BooksDataGrid.ItemsSource = _tagBooks;

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
                var tag = await LibraryService.GetTagByIdAsync(_tagId);

                if (tag == null)
                {
                    MessageBox.Show("Tag not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Load tag data
                NameTextBox.Text = tag.Name;

                // Load books in this tag
                await RefreshBooksListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tag: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async Task RefreshBooksListAsync()
        {
            var books = await LibraryService.GetBooksByTagAsync(_tagId);

            _tagBooks.Clear();

            foreach (var book in books)
            {
                _tagBooks.Add(book);
            }

            // Show "no books" message if empty
            NoBooksText.Visibility = _tagBooks.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async void RemoveBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                // Load fresh tag name for confirmation message
                var tag = await LibraryService.GetGenreByIdAsync(_tagId);
                if (tag == null)
                {
                    MessageBox.Show("Tag not found.", "Error");
                    Close();
                    return;
                }

                var result = MessageBox.Show(
                    $"Remove '{book.Title}' from the tag '{tag.Name}'?\n\n" +
                    "This will not delete the book from your library.",
                    "Confirm Removal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await LibraryService.RemoveTagFromBookAsync(book.Id, _tagId);
                        await RefreshBooksListAsync();
                        _changesMade = true;

                        MessageBox.Show($"'{book.Title}' removed from tag '{tag.Name}'.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error removing book from tag: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
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
                var tagToUpdate = await LibraryService.GetTagByIdAsync(_tagId);

                if (tagToUpdate == null)
                {
                    MessageBox.Show("Tag does not exist.", "Error");
                    Close();
                    return;
                }
                
                tagToUpdate.Name = name;
                await LibraryService.UpdateTagAsync(tagToUpdate);

                _changesMade = true;

                MessageBox.Show("Tag successfully updated!", "Changes Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating tag: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_changesMade)
                DialogResult = false;
            Close();
        }
    }
}
