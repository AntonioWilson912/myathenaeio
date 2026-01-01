using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;

namespace MyAthenaeio.Views.Collections
{
    /// <summary>
    /// Interaction logic for CollectionEditDialog.xaml
    /// </summary>
    public partial class CollectionEditDialog : Window
    {
        private readonly int _collectionId;
        private ObservableCollection<Book> _collectionBooks;
        private bool _changesMade = false;

        public CollectionEditDialog(int collectionId)
        {
            InitializeComponent();
            _collectionId = collectionId;
            _collectionBooks = [];

            BooksDataGrid.ItemsSource = _collectionBooks;

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
                var collection = await LibraryService.GetCollectionByIdAsync(_collectionId);

                if (collection == null)
                {
                    MessageBox.Show("Collection not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Load collection data
                NameTextBox.Text = collection.Name;
                DescriptionTextBox.Text = collection.Description;
                NotesTextBox.Text = collection.Notes;

                // Load books in this collection
                await RefreshBooksListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading genre: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async Task RefreshBooksListAsync()
        {
            var books = await LibraryService.GetBooksByCollectionAsync(_collectionId);

            _collectionBooks.Clear();

            foreach (var book in books)
            {
                _collectionBooks.Add(book);
            }

            // Show "no books" message if empty
            NoBooksText.Visibility = _collectionBooks.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async void RemoveBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                // Load fresh genre name for confirmation message
                var collection = await LibraryService.GetGenreByIdAsync(_collectionId);
                if (collection == null)
                {
                    MessageBox.Show("Collection not found.", "Error");
                    Close();
                    return;
                }

                var result = MessageBox.Show(
                    $"Remove '{book.Title}' from the collection '{collection.Name}'?\n\n" +
                    "This will not delete the book from your library.",
                    "Confirm Removal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await LibraryService.RemoveCollectionFromBookAsync(book.Id, _collectionId);
                        await RefreshBooksListAsync();
                        _changesMade = true;

                        MessageBox.Show($"'{book.Title}' removed from collection '{collection.Name}'.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error removing book from collection: {ex.Message}",
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
                MessageBox.Show("Collection name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var collectionToUpdate = await LibraryService.GetCollectionByIdAsync(_collectionId);

                if (collectionToUpdate == null)
                {
                    MessageBox.Show("Collection does not exist.", "Error");
                    Close();
                    return;
                }

                collectionToUpdate.Name = name;
                collectionToUpdate.Description = DescriptionTextBox.Text?.Trim();
                collectionToUpdate.Notes = NotesTextBox.Text?.Trim();

                await LibraryService.UpdateCollectionAsync(collectionToUpdate);

                _changesMade = true;

                MessageBox.Show("Collection successfully updated!", "Changes Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating collection: {ex.Message}",
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
