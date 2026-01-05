using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using MyAthenaeio.Views.Books;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MyAthenaeio.Views.Genres
{
    /// <summary>
    /// Interaction logic for GenreDetailWindow.xaml
    /// </summary>
    public partial class GenreDetailWindow : Window
    {
        private int _genreId;
        private readonly ObservableCollection<Book> _books;
        private readonly int? _parentBookId;
        private bool _changesMade = false;

        public GenreDetailWindow(int genreId, int? parentBookId = null)
        {
            InitializeComponent();
            _genreId = genreId;
            _parentBookId = parentBookId;
            _books = [];

            BooksListBox.ItemsSource = _books;

            Loaded += async (s, e) => await LoadGenreDataAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadGenreDataAsync()
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

                NameTextBlock.Text = genre.Name;

                var books = await LibraryService.GetBooksByGenreAsync(_genreId);

                _books.Clear();
                foreach (var book in books)
                {
                    _books.Add(book);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading genre: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GenreEditDialog(_genreId) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _ = LoadGenreDataAsync();
                _changesMade = true;
            }
        }

        private void BooksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book book)
            {
                if (_parentBookId.HasValue && book.Id == _parentBookId.Value)
                {
                    MessageBox.Show("This book is already open in another window.",
                        "Book Already Open",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var bookDetailWindow = new BookDetailWindow(book.Id) { Owner = this };
                if (bookDetailWindow.ShowDialog() == true)
                {
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
