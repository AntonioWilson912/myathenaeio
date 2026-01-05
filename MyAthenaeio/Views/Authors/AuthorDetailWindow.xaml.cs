using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using MyAthenaeio.Views.Books;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Views.Authors
{
    /// <summary>
    /// Interaction logic for AuthorDetailWindow.xaml
    /// </summary>
    public partial class AuthorDetailWindow : Window
    {
        private Author? _author;
        private readonly int _authorId;
        private readonly int? _parentBookId;
        private bool _changesMade = false;

        public AuthorDetailWindow(int authorId, int? parentBookId = null)
        {
            InitializeComponent();
            _authorId = authorId;
            _parentBookId = parentBookId;

            Loaded += async (s, e) => await LoadAuthorDataAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        public async void RefreshData()
        {
            await LoadAuthorDataAsync();
        }

        private async Task LoadAuthorDataAsync()
        {
            _author = await LibraryService.GetAuthorByIdAsync(_authorId);

            if (_author == null)
            {
                MessageBox.Show("Author not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            NameTextBlock.Text = _author.Name;

            OLKeyTextBlock.Text = string.IsNullOrEmpty(_author.OpenLibraryKey)
                ? "Open Library Key: Not available"
                : $"Open Library Key: {_author.OpenLibraryKey}";

            BirthDateTextBlock.Text = _author.BirthDate == null
                ? "Birth Date: Unknown"
                : $"Birth Date: {_author.BirthDate:yyyy-MM-dd}";

            BioTextBlock.Text = string.IsNullOrEmpty(_author.Bio)
                ? "No biography available."
                : _author.Bio;

            // Load photo if available
            if (!string.IsNullOrEmpty(_author.PhotoUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_author.PhotoUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    if (bitmap.CanFreeze)
                        bitmap.Freeze();

                    PhotoImage.Source = bitmap;
                }
                catch
                {
                    // Photo failed to load - silently ignore
                }
            }

            var books = await LibraryService.GetBooksByAuthorAsync(_author.Id);
            BooksListBox.ItemsSource = books;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthorEditDialog(_author!.Id) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                // Refresh the display
                _changesMade = true;
                RefreshData();
            }
        }

        private void BooksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BooksListBox.SelectedItem is not Book book)
                return;

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
                RefreshData();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}