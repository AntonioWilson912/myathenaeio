using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Authors
{
    /// <summary>
    /// Interaction logic for AuthorAddDialog.xaml
    /// </summary>
    public partial class AuthorAddDialog : Window
    {
        private readonly List<Author> _existingBookAuthors;
        private readonly HttpClient _httpClient;
        private List<Author> _allAuthors;
        public Author? SelectedAuthor { get; private set; }

        public AuthorAddDialog(List<Author> existingBookAuthors)
        {
            InitializeComponent();
            _existingBookAuthors = existingBookAuthors;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _allAuthors = new List<Author>();

            Loaded += async (s, e) => await LoadAuthorsAsync();
        }

        private async Task LoadAuthorsAsync()
        {
            _allAuthors = await LibraryService.GetAllAuthorsAsync();
            _allAuthors.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            FilterAuthors();
        }

        private void FilterAuthors(string? searchTerm = null)
        {
            var filtered = _allAuthors.AsEnumerable();

            // Exclude authors already associated with the book
            filtered = filtered.Where(a => !_existingBookAuthors.Any(ea =>
                ea.Id == a.Id ||
                (!string.IsNullOrEmpty(a.OpenLibraryKey) && a.OpenLibraryKey == ea.OpenLibraryKey)));

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                filtered = filtered.Where(a =>
                    a.Name.ToLower().Contains(lowerSearch) ||
                    (!string.IsNullOrEmpty(a.OpenLibraryKey) && a.OpenLibraryKey.ToLower().Contains(lowerSearch)));
            }

            ExistingAuthorsListBox.ItemsSource = filtered.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAuthors(SearchTextBox.Text);
        }

        private void ExistingAuthorsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExistingAuthorsListBox.SelectedItem is Author author)
            {
                SelectAuthor(author);
            }
        }

        private async void FetchOLData_Click(object sender, RoutedEventArgs e)
        {
            var olKey = OLKeyTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(olKey))
            {
                MessageBox.Show("Please enter an Open Library key.", "Key Required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check if author with this OL key already exists
            var existingAuthor = await LibraryService.GetAuthorByOpenLibraryKeyAsync(olKey);
            if (existingAuthor != null)
            {
                // Check if already associated with book
                if (_existingBookAuthors.Any(a => a.Id == existingAuthor.Id || a.OpenLibraryKey == olKey))
                {
                    MessageBox.Show($"Author '{existingAuthor.Name}' with this Open Library key is already associated with this book.",
                        "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var result = MessageBox.Show($"Author '{existingAuthor.Name}' already exists with this Open Library key.\n\nDo you want to add this author to the book?",
                        "Author Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SelectAuthor(existingAuthor);
                    }
                }
                return;
            }

            // Validate format
            if (!olKey.StartsWith("OL") || !olKey.EndsWith("A"))
            {
                MessageBox.Show("Invalid Open Library author key format. Author keys should start with 'OL' and end with 'A' (e.g., OLA12345A).",
                    "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                FetchOLDataButton.IsEnabled = false;
                FetchOLDataButton.Content = "Fetching...";

                var authorData = await FetchAuthorFromOLAsync(olKey);

                if (authorData != null)
                {
                    NameTextBox.Text = authorData.Name;
                    BioTextBox.Text = authorData.Bio;
                    //BirthDateTextBox.Text = authorData.Item3;
                    //PhotoUrlTextBox.Text = authorData.Item4;

                    AddButton.IsEnabled = !string.IsNullOrWhiteSpace(NameTextBox.Text);

                    MessageBox.Show("Author information successfully fetched from Open Library!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Could not fetch author information. Please check the Open Library key.",
                        "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching author data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FetchOLDataButton.IsEnabled = true;
                FetchOLDataButton.Content = "Fetch Data";
            }
        }

        private async Task<AuthorInfo?> FetchAuthorFromOLAsync(string olKey)
        {
            try
            {
                var url = $"https://openlibrary.org/authors/{olKey}.json";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                var name = json["name"]?.ToString() ?? "";
                var bio = "";
                //var birthDate = "";
                //var photoUrl = "";

                // Get bio
                if (json["bio"] is JToken bioToken)
                {
                    if (bioToken.Type == JTokenType.String)
                    {
                        bio = bioToken.ToString();
                    }
                    else if (bioToken.Type == JTokenType.Object && bioToken["value"] != null)
                    {
                        bio = bioToken["value"]?.ToString() ?? "";
                    }
                }

                //// Get birth date
                //birthDate = DateNormalizer.NormalizeDate(json["birth_date"]?.ToString());

                //// Get photo
                //if (json["photos"] is JArray photos && photos.Count > 0)
                //{
                //    var photoId = photos[0].ToString();
                //    photoUrl = $"https://covers.openlibrary.org/a/id/{photoId}-M.jpg";
                //}

                return new AuthorInfo()
                {
                    Name = name,
                    Bio = bio
                };
            }
            catch
            {
                return null;
            }
        }

        private void SelectAuthor(Author author)
        {
            SelectedAuthor = author;
            DialogResult = true;
            Close();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Author name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var olKey = string.IsNullOrWhiteSpace(OLKeyTextBox.Text) ? null : OLKeyTextBox.Text.Trim();

                // Check if author already exists
                var existingAuthor = await LibraryService.GetAuthorByNameAsync(name);

                if (existingAuthor != null)
                {
                    // If OL keys don't match, it might be a different author with same name
                    if (!string.IsNullOrEmpty(olKey) && existingAuthor.OpenLibraryKey != olKey)
                    {
                        var result = MessageBox.Show(
                            $"An author named '{name}' already exists with a different Open Library key.\n\n" +
                            "Do you want to create a new author entry? (This could be a different person with the same name)",
                            "Duplicate Name",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                            return;
                    }
                    else
                    {
                        // Same author, just use existing
                        SelectAuthor(existingAuthor);
                        return;
                    }
                }

                // Check if OL key is already used
                if (!string.IsNullOrEmpty(olKey))
                {
                    var authorWithKey = await LibraryService.GetAuthorByOpenLibraryKeyAsync(olKey);
                    if (authorWithKey != null)
                    {
                        MessageBox.Show($"An author with Open Library key '{olKey}' already exists: {authorWithKey.Name}",
                            "Duplicate Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Create new author
                var newAuthor = new Author
                {
                    Name = name,
                    OpenLibraryKey = olKey,
                    Bio = string.IsNullOrWhiteSpace(BioTextBox.Text) ? null : BioTextBox.Text.Trim()
                };

                await LibraryService.AddAuthorAsync(newAuthor);
                SelectAuthor(newAuthor);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding author: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _httpClient?.Dispose();
        }
    }
}
