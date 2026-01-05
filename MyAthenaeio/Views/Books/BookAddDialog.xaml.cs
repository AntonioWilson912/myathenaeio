using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Views.Books
{
    public partial class BookAddDialog : Window
    {
        private readonly Book _book;

        public BookAddDialog()
        {
            InitializeComponent();
            _book = new Book();
        }

        public BookAddDialog(string? prefilledISBN = null) : this()
        {
            if (!string.IsNullOrEmpty(prefilledISBN))
            {
                // Clean the ISBN first to remove any formatting
                string cleaned = ISBNValidator.CleanISBN(prefilledISBN);

                // Validate the format
                if (ISBNValidator.IsValidISBNFormat(cleaned))
                {
                    if (cleaned.Length == 10)
                    {
                        ISBN10TextBox.Text = cleaned;
                        string? isbn13 = ISBNValidator.ConvertISBN10ToISBN13(cleaned);
                        if (isbn13 != null)
                        {
                            ISBN13TextBox.Text = isbn13;
                        }
                    }
                    else if (cleaned.Length == 13)
                    {
                        ISBN13TextBox.Text = cleaned;
                        string? isbn10 = ISBNValidator.ConvertISBN13ToISBN10(cleaned);
                        if (isbn10 != null)
                        {
                            ISBN10TextBox.Text = isbn10;
                        }
                    }
                }
                else
                {
                    // Invalid ISBN - still pre-fill but warn user
                    if (cleaned.Length == 10 || cleaned.StartsWith("978") || cleaned.StartsWith("979"))
                    {
                        // Looks like it's trying to be ISBN-13
                        ISBN13TextBox.Text = cleaned;
                    }
                    else
                    {
                        // Assume ISBN-10
                        ISBN10TextBox.Text = cleaned;
                    }

                    MessageBox.Show(
                        $"The provided ISBN appears to be invalid. Please verify it before saving.\n\nISBN: {cleaned}",
                        "Invalid ISBN Format",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
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

                // Check for valid publication year
                var yearText = PublicationYearTextBox.Text.Trim();

                if (string.IsNullOrEmpty(yearText))
                {
                    _book.PublicationYear = null;
                }
                else if (int.TryParse(yearText, out int year) && year <= DateTime.Now.Year + 1)
                {
                    _book.PublicationYear = year;
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
