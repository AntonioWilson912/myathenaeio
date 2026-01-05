using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using MyAthenaeio.Views.Authors;
using MyAthenaeio.Views.BookCopies;
using MyAthenaeio.Views.Loans;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Views.Books
{
    public partial class BookDetailWindow : Window
    {
        private readonly int _bookId;
        private Book? _book;
        private ObservableCollection<BookCopy> _copies = [];
        private ObservableCollection<Author> _authors = [];
        private ObservableCollection<Genre> _genres = [];
        private ObservableCollection<Tag> _tags = [];
        private ObservableCollection<Collection> _collections = [];
        private ObservableCollection<Loan> _loans = [];
        private bool _changesMade = false;

        public BookDetailWindow(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;

            Loaded += async (s, e) => await LoadBookDataAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadBookDataAsync()
        {
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
                PublicationYearTextBox.Text = _book.PublicationYear.ToString();

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

                _authors = new ObservableCollection<Author>(_book.Authors);
                _genres = new ObservableCollection<Genre>(_book.Genres);
                _tags = new ObservableCollection<Tag>(_book.Tags);
                _collections = new ObservableCollection<Collection>(_book.Collections);

                _copies = new ObservableCollection<BookCopy>(_book.Copies);

                _loans = [];

                foreach (var copy in _book.Copies)
                {
                    foreach (var loan in copy.Loans)
                    {
                        _loans.Add(loan);
                    }
                }

                AuthorsDataGrid.ItemsSource = _authors;
                _ = LoadAuthors();
                LoadGenres();
                LoadTags();
                LoadCollections();
                CopiesDataGrid.ItemsSource = _copies;
                LoanHistoryDataGrid.ItemsSource = _loans.OrderByDescending(l => l.CheckoutDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading book data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadAuthors()
        {
            var authors = await LibraryService.GetAuthorsByBookAsync(_bookId);

            _authors.Clear();
            foreach (var author in authors)
            {
                _authors.Add(author);
            }

            AuthorsTextBox.Text = string.Join(", ", _authors.Select(a => a.Name));
        }

        private void LoadGenres()
        {
            GenresWrapPanel.Children.Clear();

            foreach (var genre in _genres)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(2)
                };

                var button = new Button
                {
                    Content = genre.Name,
                    Padding = new Thickness(8, 4, 8, 4),
                    Style = (Style)FindResource("TagButtonStyle"),
                    Tag = genre
                };
                button.Click += GenreButton_Click;

                var removeButton = new Button
                {
                    Content = "×",
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(2, 0, 0, 0),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Tag = genre,
                    ToolTip = "Remove genre"
                };
                removeButton.Click += RemoveGenre_Click;

                panel.Children.Add(button);
                panel.Children.Add(removeButton);

                GenresWrapPanel.Children.Add(panel);
            }
        }

        private void LoadTags()
        {
            TagsWrapPanel.Children.Clear();

            foreach (var tag in _tags)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(2)
                };

                var button = new Button
                {
                    Content = tag.Name,
                    Padding = new Thickness(8, 4, 8, 4),
                    Style = (Style)FindResource("TagButtonStyle"),
                    Tag = tag
                };
                button.Click += TagButton_Click;

                var removeButton = new Button
                {
                    Content = "×",
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(2, 0, 0, 0),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Tag = tag,
                    ToolTip = "Remove tag"
                };
                removeButton.Click += RemoveTag_Click;

                panel.Children.Add(button);
                panel.Children.Add(removeButton);

                TagsWrapPanel.Children.Add(panel);
            }
        }

        private void LoadCollections()
        {
            CollectionsWrapPanel.Children.Clear();

            foreach (var collection in _collections)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(2)
                };

                var button = new Button
                {
                    Content = collection.Name,
                    Padding = new Thickness(8, 4, 8, 4),
                    Style = (Style)FindResource("TagButtonStyle"),
                    Tag = collection
                };
                button.Click += CollectionButton_Click;

                var removeButton = new Button
                {
                    Content = "×",
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(2, 0, 0, 0),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Tag = collection,
                    ToolTip = "Remove collection"
                };
                removeButton.Click += RemoveCollection_Click;

                panel.Children.Add(button);
                panel.Children.Add(removeButton);

                CollectionsWrapPanel.Children.Add(panel);
            }
        }

        private async void RemoveGenre_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Genre genre)
                return;

            var result = MessageBox.Show(
                $"Remove genre '{genre.Name}' from this book?",
                "Confirm Remove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.RemoveGenreFromBookAsync(_bookId, genre.Id);
                _genres.Remove(genre);
                LoadGenres();
                _changesMade = true;

                MessageBox.Show(
                    $"Genre '{genre.Name}' removed from book.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error removing genre: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Tag tag)
                return;

            var result = MessageBox.Show(
                $"Remove tag '{tag.Name}' from this book?",
                "Confirm Remove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.RemoveTagFromBookAsync(_bookId, tag.Id);
                _tags.Remove(tag);
                LoadTags();
                _changesMade = true;

                MessageBox.Show(
                    $"Tag '{tag.Name}' removed from book.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error removing tag: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RemoveCollection_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Collection collection)
                return;

            var result = MessageBox.Show(
                $"Remove collection '{collection.Name}' from this book?",
                "Confirm Remove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.RemoveCollectionFromBookAsync(_bookId, collection.Id);
                _collections.Remove(collection);
                LoadCollections();
                _changesMade = true;

                MessageBox.Show(
                    $"Collection '{collection.Name}' removed from book.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error removing collection: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void AddGenre_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Genres.GenreAddDialog(_bookId) { Owner = this };

            if (dialog.ShowDialog() == true && dialog.SelectedGenre != null)
            {
                try
                {
                    // Check if genre is already associated
                    if (_genres.Any(g => g.Id == dialog.SelectedGenre.Id))
                    {
                        MessageBox.Show("This genre is already associated with this book.", "Already Added",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    await LibraryService.AddGenreToBookAsync(_bookId, dialog.SelectedGenre.Id);
                    _genres.Add(dialog.SelectedGenre);
                    LoadGenres();
                    _changesMade = true;

                    MessageBox.Show($"Genre '{dialog.SelectedGenre.Name}' added to book.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding genre: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenreButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var genre = (Genre)button.Tag;

            var detailWindow = new Genres.GenreDetailWindow(genre.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                _ = LoadBookDataAsync();
                _changesMade = true;
            }
        }

        private async void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Tags.TagAddDialog(_bookId) { Owner = this };

            if (dialog.ShowDialog() == true && dialog.SelectedTag != null)
            {
                try
                {
                    // Check if tag is already associated
                    if (_tags.Any(t => t.Id == dialog.SelectedTag.Id))
                    {
                        MessageBox.Show("This tag is already associated with this book.", "Already Added",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    await LibraryService.AddTagToBookAsync(_bookId, dialog.SelectedTag.Id);
                    _tags.Add(dialog.SelectedTag);
                    LoadTags();
                    _changesMade = true;

                    MessageBox.Show($"Tag '{dialog.SelectedTag.Name}' added to book.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding tag: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tag = (Tag)button.Tag;

            var detailWindow = new Tags.TagDetailWindow(tag.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                _ = LoadBookDataAsync();
                _changesMade = true;
            }
        }

        private async void AddCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Collections.CollectionAddDialog(_bookId) { Owner = this };

            if (dialog.ShowDialog() == true && dialog.SelectedCollection != null)
            {
                try
                {
                    // Check if collection is already associated
                    if (_collections.Any(c => c.Id == dialog.SelectedCollection.Id))
                    {
                        MessageBox.Show("This collection is already associated with this book.", "Already Added",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    await LibraryService.AddCollectionToBookAsync(_bookId, dialog.SelectedCollection.Id);
                    _collections.Add(dialog.SelectedCollection);
                    LoadCollections();
                    _changesMade = true;

                    MessageBox.Show($"Collection '{dialog.SelectedCollection.Name}' added to book.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding collection: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CollectionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var collection = (Collection)button.Tag;

            var detailWindow = new Collections.CollectionDetailWindow(collection.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                _ = LoadBookDataAsync();
                _changesMade = true;
            }
        }

        private async void AuthorsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AuthorsDataGrid.SelectedItem is Author author)
            {
                var detailWindow = new AuthorDetailWindow(author.Id) { Owner = this };

                if (detailWindow.ShowDialog() == true)
                {
                    _ = LoadAuthors();
                    _changesMade = true;
                }
            }
        }

        private async void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthorAddDialog([.. _authors]) { Owner = this };

            if (dialog.ShowDialog() == true && dialog.SelectedAuthor != null)
            {
                try
                {
                    // Check if author is already associated
                    if (_authors.Any(a => a.Id == dialog.SelectedAuthor.Id))
                    {
                        MessageBox.Show("This author is already associated with this book.", "Already Added",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    List<int> authorIds = [.. _authors.Select(a => a.Id)];
                    authorIds.Add(dialog.SelectedAuthor.Id);

                    await LibraryService.UpdateBookAuthorsAsync(_bookId, authorIds);

                    _ = LoadAuthors();
                    _changesMade = true;
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

                _ = LoadAuthors();
                _changesMade = true;

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

        private async void AddCopy_Click(object sender, RoutedEventArgs e)
        {
            var copyNumber = (_copies.Count + 1).ToString();
            var copy = new BookCopy
            {
                BookId = _bookId,
                CopyNumber = copyNumber,
                AcquisitionDate = DateTime.Now,
                IsAvailable = true
            };

            copy = await LibraryService.AddBookCopyAsync(_bookId, copy);

            _copies.Add(copy);

            _changesMade = true;
        }

        private void CopiesDataGrid_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("Book copy has been double-clicked");
            if (CopiesDataGrid.SelectedItem is BookCopy copy)
            {
                var copyDetailWindow = new BookCopyDetailWindow(copy.Id) { Owner = this };

                if (copyDetailWindow.ShowDialog() == true)
                {
                    _ = LoadBookDataAsync();
                    _changesMade = true;
                }
            }
        }

        private void ManageLoans_Click(object sender, RoutedEventArgs e)
        {
            var loanManagementWindow = new LoanManagementWindow(_bookId) { Owner = this };
            if (loanManagementWindow.ShowDialog() == true)
            {
                _ = LoadBookDataAsync();
                _changesMade = true;
            }
        }

        private void LoanHistoryDataGrid_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (LoanHistoryDataGrid.SelectedItem is Loan loan)
            {
                var loanDetailWindow = new LoanDetailWindow(loan.Id) { Owner = this };

                if (loanDetailWindow.ShowDialog() == true)
                {
                    _ = LoadBookDataAsync();
                    _changesMade = true;
                }
            }
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

                // Check for duplicate ISBN
                var bookIsbn10 = await LibraryService.GetBookByISBNAsync(isbn10);
                var bookIsbn13 = await LibraryService.GetBookByISBNAsync(isbn13);

                if ((bookIsbn10 != null && _book.Id != bookIsbn10.Id) ||
                    (bookIsbn13 != null && _book.Id != bookIsbn13.Id))
                {
                    MessageBox.Show("A book with this ISBN already exists.",
                        "Duplicate Book",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get fresh copy from database to update
                var bookToUpdate = await LibraryService.GetBookByIdAsync(_bookId);
                if (bookToUpdate == null)
                {
                    MessageBox.Show("Book not found.", "Error");
                    Close();
                    return;
                }

                // Update properties
                bookToUpdate.ISBN = (!string.IsNullOrEmpty(isbn13)) ? isbn13 :
                                    (!string.IsNullOrEmpty(isbn10)) ? ISBNValidator.ConvertISBN10ToISBN13(isbn10)! :
                                    bookToUpdate.ISBN;
                bookToUpdate.Title = TitleTextBox.Text.Trim();
                bookToUpdate.Subtitle = SubtitleTextBox.Text.Trim();
                bookToUpdate.Publisher = PublisherTextBox.Text.Trim();
                bookToUpdate.Description = DescriptionTextBox.Text.Trim();
                bookToUpdate.CoverImageUrl = CoverUrlTextBox.Text.Trim();

                // Check for valid publication year
                var yearText = PublicationYearTextBox.Text.Trim();

                if (string.IsNullOrEmpty(yearText))
                {
                    bookToUpdate.PublicationYear = null;
                }
                else if (int.TryParse(yearText, out int year) && year <= DateTime.Now.Year + 1)
                {
                    bookToUpdate.PublicationYear = year;
                }
                else
                {
                    MessageBox.Show(
                        $"Please enter a valid year before {DateTime.Now.Year + 1}.",
                        "Invalid Year",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    PublicationYearTextBox.Focus();
                    return;
                }

                // Update the book
                await LibraryService.UpdateBookAsync(bookToUpdate);

                MessageBox.Show("Book saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving book: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(ex.InnerException?.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_changesMade)
                DialogResult = false;
            Close();
        }

        private static bool ValidateCoverUrl(string url)
        {
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!result)
                return false;

            string path = uriResult!.AbsolutePath.ToLower();
            string[] validExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];

            return validExtensions.Any(ext => path.Contains(ext));
        }
    }
}
