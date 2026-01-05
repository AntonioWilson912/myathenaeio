using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MyAthenaeio.Views.Genres
{
    public partial class GenreEditDialog : Window
    {
        private readonly int _genreId;
        private ObservableCollection<Book> _genreBooks;
        private bool _changesMade = false;

        public GenreEditDialog(int genreId)
        {
            InitializeComponent();
            _genreId = genreId;
            _genreBooks = [];

            BooksDataGrid.ItemsSource = _genreBooks;

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
                var genre = await LibraryService.GetGenreByIdAsync(_genreId);

                if (genre == null)
                {
                    MessageBox.Show("Genre not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Load genre data
                NameTextBox.Text = genre.Name;

                // Load books in this genre
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
            var books = await LibraryService.GetBooksByGenreAsync(_genreId);

            _genreBooks.Clear();

            foreach (var book in books)
            {
                _genreBooks.Add(book);
            }

            // Show "no books" message if empty
            NoBooksText.Visibility = _genreBooks.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async void RemoveBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                // Load fresh genre name for confirmation message
                var genre = await LibraryService.GetGenreByIdAsync(_genreId);
                if (genre == null)
                {
                    MessageBox.Show("Genre not found.", "Error");
                    Close();
                    return;
                }

                var result = MessageBox.Show(
                    $"Remove '{book.Title}' from the genre '{genre!.Name}'?\n\n" +
                    "This will not delete the book from your library.",
                    "Confirm Removal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await LibraryService.RemoveGenreFromBookAsync(book.Id, _genreId);
                        await RefreshBooksListAsync();
                        _changesMade = true;

                        MessageBox.Show($"'{book.Title}' removed from genre '{genre.Name}'.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error removing book from genre: {ex.Message}",
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
                MessageBox.Show("Genre name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var genreToUpdate = await LibraryService.GetGenreByIdAsync(_genreId);

                if (genreToUpdate == null)
                {
                    MessageBox.Show("Genre does not exist.", "Error");
                    Close();
                    return;
                }

                genreToUpdate.Name = name;
                await LibraryService.UpdateGenreAsync(genreToUpdate);

                MessageBox.Show("Genre successfully updated!", "Changes Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _changesMade = true;
                Close();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating genre: {ex.Message}",
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