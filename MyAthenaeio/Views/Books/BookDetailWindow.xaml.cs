using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using MyAthenaeio.Views.Authors;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Views.Books
{
    public partial class BookDetailWindow : Window
    {
        private int _bookId;
        private Book? _book;
        private ObservableCollection<BookCopy> _copies = [];
        private ObservableCollection<Author> _authors = [];
        private ObservableCollection<Loan> _loans = [];
        private bool _isClosing = false;

        public BookDetailWindow(Book book)
        {
            InitializeComponent();
            _bookId = book.Id;

            Loaded += async (s, e) => await LoadBookDataAsync();
        }

        private async Task LoadBookDataAsync()
        {
            if (_isClosing) return;

            try
            {
                _book = await LibraryService.GetBookByIdAsync(_bookId, BookIncludeOptions.Full);
                if (_book == null)
                {
                    MessageBox.Show("Book not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                int isbnLength = _book.ISBN.Length;

                TitleTextBox.Text = _book.Title;
                SubtitleTextBox.Text = _book.Subtitle;
                ISBN10TextBox.Text = isbnLength == 10 ? _book.ISBN : ISBNValidator.ConvertISBN13ToISBN10(_book.ISBN) ?? "";
                ISBN13TextBox.Text = isbnLength == 13 ? _book.ISBN : ISBNValidator.ConvertISBN10ToISBN13(_book.ISBN) ?? "";
                PublisherTextBox.Text = _book.Publisher;
                DescriptionTextBox.Text = _book.Description;
                CoverUrlTextBox.Text = _book.CoverImageUrl;

                if (_book.PublicationYear.HasValue)
                {
                    PublicationDatePicker.SelectedDate = new DateTime(_book.PublicationYear.Value, 1, 1);
                }

                if (!string.IsNullOrEmpty(_book.CoverImageUrl))
                {
                    try
                    {
                        CoverImage.Source = new BitmapImage(new Uri(_book.CoverImageUrl));
                        NoCoverText.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        NoCoverText.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    NoCoverText.Visibility = Visibility.Visible;
                }

                LoadGenres();
                LoadTags();
                LoadCollections();

                _authors = new ObservableCollection<Author>(_book.Authors);

                AuthorsTextBox.Text = string.Join(", ", _authors.Select(a => a.Name));

                _copies = new ObservableCollection<BookCopy>(_book.Copies);

                _loans = new ObservableCollection<Loan>();

                foreach (var copy in _book.Copies)
                {
                    foreach (var loan in copy.Loans)
                    {
                        _loans.Add(loan);
                    }
                }

                AuthorsDataGrid.ItemsSource = _authors;
                CopiesDataGrid.ItemsSource = _copies;
                LoanHistoryDataGrid.ItemsSource = _loans.OrderByDescending(l => l.CheckoutDate);
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    MessageBox.Show($"Error loading book data: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadGenres()
        {
            if (_book == null) return;

            GenresWrapPanel.Children.Clear();

            foreach (var genre in _book.Genres)
            {
                var button = new Button
                {
                    Content = genre.Name,
                    Margin = new Thickness(2),
                    Padding = new Thickness(8, 4, 8, 4),
                    Style = (Style)FindResource("TagButtonStyle"),
                    Tag = genre
                };
                button.Click += GenreButton_Click;

                GenresWrapPanel.Children.Add(button);
            }
        }

        private void LoadTags()
        {
            if (_book == null) return;

            TagsWrapPanel.Children.Clear();

            foreach (var tag in _book.Tags)
            {
                var button = new Button
                {
                    Content = tag.Name,
                    Margin = new Thickness(2),
                    Padding = new Thickness(8, 4, 8, 4),
                    Style = (Style)FindResource("TagButtonStyle"),
                    Tag = tag
                };
                button.Click += TagButton_Click;

                TagsWrapPanel.Children.Add(button);
            }
        }

        private void LoadCollections()
        {
            if (_book == null) return;

            CollectionsWrapPanel.Children.Clear();

            foreach (var collection in _book.Collections)
            {
                var button = new Button
                {
                    Content = collection.Name,
                    Margin = new Thickness(2),
                    Padding = new Thickness(8, 4, 8, 4),
                    Style = (Style)FindResource("TagButtonStyle"),
                    Tag = collection
                };
                button.Click += CollectionButton_Click;

                CollectionsWrapPanel.Children.Add(button);
            }
        }

        private void AddGenre_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GenreButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var genre = (Genre)button.Tag;

            var detailWindow = new Genres.GenreDetailWindow(genre)
            {
                Owner = this
            };
            detailWindow.ShowDialog();
            Debug.WriteLine($"Genre {genre.Name} with ID {genre.Id} clicked");
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            // var addDialog = new TagAddDialog();
            // addDialog.ShowDialog();
            // RefreshTags();
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tag = (Tag)button.Tag;

            var detailWindow = new Tags.TagDetailWindow(tag)
            {
                Owner = this
            };
            Debug.WriteLine($"Tag {tag.Name} with ID {tag.Id} clicked");
        }

        private void AddCollection_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CollectionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var collection = (Collection)button.Tag;

            var detailWindow = new Collections.CollectionDetailWindow(collection)
            {
                Owner = this
            };
            Debug.WriteLine($"Collection {collection.Name} with ID {collection.Id} clicked");
        }

        private void AuthorsDataGrid_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // Show the clicked author
        }

        private async void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthorAddDialog([.. _authors]);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && dialog.SelectedAuthor != null)
            {
                try
                {
                    if (_book == null) return;

                    // Check if author is already associated
                    if (_authors.Any(a => a.Id == dialog.SelectedAuthor.Id))
                    {
                        MessageBox.Show("This author is already associated with this book.", "Already Added",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    List<int> authorIds = [.. _book.Authors.Select(a => a.Id)];
                    authorIds.Add(dialog.SelectedAuthor.Id);

                    await LibraryService.UpdateBookAuthorsAsync(_bookId, authorIds);
                    _authors.Add(dialog.SelectedAuthor);
                    AuthorsDataGrid.Items.Refresh();
                    AuthorsTextBox.Text = string.Join(", ", _authors.Select(a => a.Name));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding author: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not Author author)
                return;

            var result = MessageBox.Show(
                $"Remove '{author.Name}' from this book?",
                "Confirm Remove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Remove author from book using LibraryService
                var remainingAuthorIds = _authors
                    .Where(a => a.Id != author.Id)
                    .Select(a => a.Id)
                    .ToList();

                await LibraryService.UpdateBookAuthorsAsync(_bookId, remainingAuthorIds);

                // Update local collection
                _authors.Remove(author);
                AuthorsDataGrid.Items.Refresh();
                AuthorsTextBox.Text = string.Join(", ", _authors.Select(a => a.Name));

                MessageBox.Show(
                    $"'{author.Name}' removed from this book.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error removing author: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RefreshAuthors_Click(object sender, RoutedEventArgs e)
        {
            // Refresh authors
        }

        private void AddCopy_Click(object sender, RoutedEventArgs e)
        {
            // Add copy
        }

        private void ManageLoans_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LoanHistoryDataGrid_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // Show the clicked loan
        }

        private void ISBN10TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var isbn10 = ISBNValidator.CleanISBN(ISBN10TextBox.Text);

            if (!string.IsNullOrEmpty(isbn10) && isbn10.Length == 10)
            {
                // Validate ISBN-10
                if (ISBNValidator.ValidateISBN10(isbn10))
                {
                    // Convert to ISBN-13 if ISBN-13 field is empty
                    if (string.IsNullOrWhiteSpace(ISBN13TextBox.Text))
                    {
                        var isbn13 = ISBNValidator.ConvertISBN10ToISBN13(isbn10);
                        if (isbn13 != null)
                            ISBN13TextBox.Text = isbn13;
                    }
                }
                else
                {
                    MessageBox.Show("Invalid ISBN-10 format. Please check the number.",
                        "Invalid ISBN", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ISBN13TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var isbn13 = ISBNValidator.CleanISBN(ISBN13TextBox.Text);

            if (!string.IsNullOrEmpty(isbn13) && isbn13.Length == 13)
            {
                // Validate ISBN-13
                if (ISBNValidator.ValidateISBN13(isbn13))
                {
                    // Convert to ISBN-10 if ISBN-10 field is empty and it's convertible
                    if (string.IsNullOrWhiteSpace(ISBN10TextBox.Text) && isbn13.StartsWith("978"))
                    {
                        var isbn10 = ISBNValidator.ConvertISBN13ToISBN10(isbn13);
                        if (isbn10 != null)
                            ISBN10TextBox.Text = isbn10;
                    }
                }
                else
                {
                    MessageBox.Show("Invalid ISBN-13 format. Please check the number.",
                        "Invalid ISBN", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void FetchCover_Click(object sender, RoutedEventArgs e)
        {
            var coverUrl = CoverUrlTextBox.Text;

            if (!ValidateCoverUrl(coverUrl))
            {
                MessageBox.Show("Invalid image URL.", "Image URL Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                CoverImage.Source = new BitmapImage(new Uri(coverUrl));
                NoCoverText.Visibility = Visibility.Collapsed;
            }
            catch
            {
                NoCoverText.Visibility = Visibility.Visible;
                MessageBox.Show("Could not load cover image.",
                    "Cover Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_book == null) return;

                if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                {
                    MessageBox.Show("Title is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ISBN10TextBox.Text) &&
                    string.IsNullOrWhiteSpace(ISBN13TextBox.Text))
                {
                    MessageBox.Show("At least one ISBN is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var isbn10 = ISBNValidator.CleanISBN(ISBN10TextBox.Text);
                var isbn13 = ISBNValidator.CleanISBN(ISBN13TextBox.Text);

                if (await LibraryService.GetBookByISBNAsync(isbn10) != null ||
                    await LibraryService.GetBookByISBNAsync(isbn13) != null)
                {
                    MessageBox.Show("A book with this ISBN already exists.",
                        "Duplicate Book",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _book.Title = TitleTextBox.Text.Trim();
                _book.Subtitle = SubtitleTextBox.Text.Trim();
                _book.ISBN = !string.IsNullOrEmpty(isbn13) ? isbn13 : isbn10;
                _book.Publisher = PublisherTextBox.Text.Trim();
                _book.Description = DescriptionTextBox.Text.Trim();
                _book.CoverImageUrl = CoverUrlTextBox.Text.Trim();

                if (PublicationDatePicker.SelectedDate.HasValue)
                {
                    _book.PublicationYear = PublicationDatePicker.SelectedDate.Value.Year;
                }

                var authorNames = AuthorsTextBox.Text.Trim().Split(",")
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a));

                List<AuthorInfo> authors = [];

                foreach (var authorName in authorNames)
                {
                    var author = new AuthorInfo()
                    {
                        Name = authorName
                    };

                    authors.Add(author);
                }

                await LibraryService.AddBookAsync(_book, authors);

                MessageBox.Show("Book saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving book: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static bool ValidateCoverUrl(string url)
        {
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!result)
                return false;

            string path = uriResult!.AbsolutePath.ToLower();
            string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

            return validExtensions.Any(ext => path.Contains(ext));
        }
    }
}
