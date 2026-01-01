using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using MyAthenaeio.Views.Books;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MyAthenaeio.Views.Collections
{
    /// <summary>
    /// Interaction logic for CollectionDetailWindow.xaml
    /// </summary>
    public partial class CollectionDetailWindow : Window
    {
        private readonly int _collectionId;
        private readonly ObservableCollection<Book> _books;
        private readonly int? _parentBookId;
        private bool _changesMade = false;

        public CollectionDetailWindow(int collectionId, int? parentBookId = null)
        {
            InitializeComponent();
            _collectionId = collectionId;
            _parentBookId = parentBookId;
            _books = [];

            BooksListBox.ItemsSource = _books;

            Loaded += async (s, e) => await LoadCollectionDataAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadCollectionDataAsync()
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

                NameTextBlock.Text = collection.Name;
                DescriptionTextBlock.Text = collection.Description;
                NotesTextBlock.Text = collection.Notes;

                var books = await LibraryService.GetBooksByCollectionAsync(_collectionId);

                _books.Clear();
                foreach (var book in books)
                {
                    _books.Add(book);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading collection: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CollectionEditDialog(_collectionId) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _ = LoadCollectionDataAsync();
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
                    _ = LoadCollectionDataAsync();
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
