using System.Windows;
using MyAthenaeio.Data;
using MyAthenaeio.Models;
using MyAthenaeio.Services;
using Brushes = System.Windows.Media.Brushes;

namespace MyAthenaeio
{
    /// <summary>
    /// Interaction logic for BookDetailWindow.xaml
    /// </summary>
    public partial class BookDetailWindow : Window
    {
        private BookApiResponse _book;

        public BookDetailWindow(BookApiResponse book)
        {
            InitializeComponent();
            _book = book;
            LoadBookDetails();
        }

        private void LoadBookDetails()
        {
            // Set basic info
            TitleText.Text = _book.Title;
            SubtitleText.Text = _book.Subtitle ?? string.Empty;
            SubtitleText.Visibility = string.IsNullOrEmpty(_book.Subtitle) ? Visibility.Collapsed : Visibility.Visible;

            // Authors
            if (_book.Authors != null && _book.Authors.Count > 0)
            {
                AuthorsText.Text = "by " + string.Join(", ", _book.Authors);
                AuthorsList.ItemsSource = _book.Authors;
            }
            else
            {
                AuthorsText.Text = "Unknown Author";
                AuthorsList.Visibility = Visibility.Collapsed;
            }

            // Publish date
            if (_book.PublishDate != DateTime.MinValue)
            {
                PublishDateText.Text = $"Published on {_book.PublishDate:MMMM dd, yyyy}";
            }
            else
            {
                PublishDateText.Text = "Unknown Publish Date";
            }

            // Cover
            CoverImage.Source = _book.Cover ?? BookAPIService.CreatePlaceholderImage();

            // Description
            if (!string.IsNullOrEmpty(_book.Description))
            {
                DescriptionText.Text = _book.Description;
            }
            else
            {
                DescriptionText.Text = "No description available.";
                DescriptionText.FontStyle = FontStyles.Italic;
            }

            // ISBNs
            Isbn10Text.Text = _book.Isbn10 ?? "N/A";
            Isbn13Text.Text = _book.Isbn13 ?? "N/A";

            // Key
            KeyText.Text = _book.Key;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
