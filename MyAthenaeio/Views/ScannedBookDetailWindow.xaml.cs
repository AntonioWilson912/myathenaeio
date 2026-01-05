using System.Windows;
using MyAthenaeio.Data;
using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Services;

namespace MyAthenaeio.Views
{
    /// <summary>
    /// Interaction logic for ScannedBookDetailWindow.xaml
    /// </summary>
    public partial class ScannedBookDetailWindow : Window
    {
        private readonly BookApiResponse? _book;
        private readonly string? _errorMessage;
        private readonly string _barcode;

        public ScannedBookDetailWindow(BookApiResponse book)
        {
            InitializeComponent();
            _book = book;
            _barcode = book.Isbn13 ?? book.Isbn10 ?? "Unknown";
            LoadBookDetails();
        }

        public ScannedBookDetailWindow(string barcode, string errorMessage)
        {
            InitializeComponent();
            _book = null;
            _errorMessage = errorMessage;
            _barcode = barcode;
            LoadErrorDetails();
        }

        private void LoadBookDetails()
        {
            if (_book == null) return;

            // Set basic info
            TitleText.Text = _book.Title;
            SubtitleText.Text = _book.Subtitle ?? string.Empty;
            SubtitleText.Visibility = string.IsNullOrEmpty(_book.Subtitle) ? Visibility.Collapsed : Visibility.Visible;

            // Authors
            if (_book.Authors != null && _book.Authors.Count > 0)
            {
                AuthorsText.Text = "by " + string.Join(", ", _book.Authors.ConvertAll(a => a.Name));
                AuthorsList.ItemsSource = _book.Authors;
                AuthorsList.Visibility = Visibility.Visible;
                NoAuthorsMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                AuthorsText.Text = "Unknown Author";
                AuthorsList.Visibility = Visibility.Collapsed;
                NoAuthorsMessage.Visibility = Visibility.Visible;
            }

            // Publish date
            if (_book.PublishDate.HasValue && _book.PublishDate.Value != DateTime.MinValue)
            {
                PublishDateText.Text = $"Published on {_book.PublishDate.Value:MMMM dd, yyyy}";
            }
            else
            {
                PublishDateText.Text = "Unknown Publish Date";
            }

            // Cover
            CoverImage.Source = _book.Cover ?? BookApiService.CreatePlaceholderImage();

            // Description
            if (!string.IsNullOrEmpty(_book.Description))
            {
                DescriptionText.Text = _book.Description;
                DescriptionText.FontStyle = FontStyles.Normal;
            }
            else
            {
                DescriptionText.Text = "No description available.";
                DescriptionText.FontStyle = FontStyles.Italic;
                DescriptionText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x99, 0x99, 0x99));
            }

            // ISBNs
            Isbn10Text.Text = _book.Isbn10 ?? "N/A";
            Isbn13Text.Text = _book.Isbn13 ?? "N/A";

            // Key
            KeyText.Text = _book.Key ?? "N/A";

            // Hide technical error section for successful scans
            TechnicalErrorBorder.Visibility = Visibility.Collapsed;
        }

        private void LoadErrorDetails()
        {
            Title = "Scan Failed";

            // Show error information
            TitleText.Text = "Scan Failed";
            SubtitleText.Text = $"ISBN: {_barcode}";
            SubtitleText.Visibility = Visibility.Visible;
            AuthorsText.Visibility = Visibility.Collapsed;
            PublishDateText.Visibility = Visibility.Collapsed;

            // Show error icon
            CoverImage.Source = BookApiService.CreatePlaceholderImage();

            // Update description label
            DescriptionLabel.Text = "Error Details";

            // Show user-friendly error
            DescriptionText.Text = BookApiService.GetUserFriendlyError(_errorMessage ?? "Unknown error");
            DescriptionText.FontWeight = FontWeights.SemiBold;
            DescriptionText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));

            // Show technical error details
            TechnicalErrorBorder.Visibility = Visibility.Visible;
            TechnicalErrorText.Text = _errorMessage ?? "Unknown error";

            // Hide entire author section for errors
            AuthorsSection.Visibility = Visibility.Collapsed;

            // Show ISBN info
            Isbn10Text.Text = "N/A";
            Isbn13Text.Text = _barcode;
            KeyText.Text = "N/A";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}