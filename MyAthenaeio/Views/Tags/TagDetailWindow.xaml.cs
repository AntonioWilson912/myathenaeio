using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using MyAthenaeio.Views.Books;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MyAthenaeio.Views.Tags
{
    /// <summary>
    /// Interaction logic for TagDetailWindow.xaml
    /// </summary>
    public partial class TagDetailWindow : Window
    {
        private readonly int _tagId;
        private readonly int? _parentBookId;
        private readonly ObservableCollection<Book> _tagBooks;
        private bool _changesMade = false;

        public TagDetailWindow(int tagId, int? parentBookId = null)
        {
            InitializeComponent();
            _tagId = tagId;
            _parentBookId = parentBookId;
            _tagBooks = [];

            BooksListBox.ItemsSource = _tagBooks;

            Loaded += async (s, e) => await LoadTagDataAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadTagDataAsync()
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

                NameTextBlock.Text = tag.Name;

                var books = await LibraryService.GetBooksByTagAsync(_tagId);

                _tagBooks.Clear();

                foreach (var book in books)
                {
                    _tagBooks.Add(book);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tag: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TagEditDialog(_tagId) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _ = LoadTagDataAsync();
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
