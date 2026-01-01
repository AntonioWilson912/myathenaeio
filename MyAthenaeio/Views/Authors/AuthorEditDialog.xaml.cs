using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Views.Authors
{
    public partial class AuthorEditDialog : Window, INotifyPropertyChanged
    {
        private readonly int _authorId;
        private Author? _author;
        private readonly HttpClient _httpClient;
        private bool _hasOLKey;
        private bool _changesMade = false;

        public bool HasOLKey
        {
            get => _hasOLKey;
            set
            {
                _hasOLKey = value;
                OnPropertyChanged(nameof(HasOLKey));
                OnPropertyChanged(nameof(CanFetchOLData));
            }
        }

        public bool CanFetchOLData => !_hasOLKey;

        public AuthorEditDialog(int authorId)
        {
            InitializeComponent();
            DataContext = this;
            _authorId = authorId;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            Loaded += async (s, e) => await LoadAuthorData();

            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadAuthorData()
        {
            _author = await LibraryService.GetAuthorByIdAsync(_authorId);

            if (_author == null)
            {
                MessageBox.Show("Author not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            NameTextBox.Text = _author.Name;
            OLKeyTextBox.Text = _author.OpenLibraryKey;
            BirthDatePicker.SelectedDate = _author.BirthDate;
            PhotoUrlTextBox.Text = _author.PhotoUrl;
            BioTextBox.Text = _author.Bio;

            HasOLKey = !string.IsNullOrEmpty(_author.OpenLibraryKey);

            // Enable View Photo button if photo URL exists
            ViewPhotoButton.IsEnabled = !string.IsNullOrWhiteSpace(_author.PhotoUrl);
        }

        private void PhotoUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Enable/disable View Photo button based on whether URL exists
            ViewPhotoButton.IsEnabled = !string.IsNullOrWhiteSpace(PhotoUrlTextBox.Text);
        }

        private void ViewPhoto_Click(object sender, RoutedEventArgs e)
        {
            var photoUrl = PhotoUrlTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(photoUrl))
            {
                MessageBox.Show("No photo URL available.", "No Photo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create a simple window to display the photo
            var photoWindow = new Window
            {
                Title = $"Author Photo - {_author!.Name}",
                Width = 400,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.CanResize
            };

            var grid = new Grid();

            var image = new Image
            {
                Stretch = System.Windows.Media.Stretch.Uniform,
                Margin = new Thickness(10)
            };

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                image.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load photo: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            grid.Children.Add(image);
            photoWindow.Content = grid;
            photoWindow.ShowDialog();
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

            // Validate format (should be like OLA12345A for authors)
            if (!olKey.StartsWith("OL") || !olKey.EndsWith('A'))
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
                    // Validate that the name loosely matches
                    var fetchedName = authorData.Name;
                    if (!IsNameSimilar(_author!.Name, fetchedName))
                    {
                        var result = MessageBox.Show(
                            $"The Open Library author name '{fetchedName}' doesn't closely match '{_author.Name}'.\n\n" +
                            "Do you still want to use this data?",
                            "Name Mismatch",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }

                    // Update fields with fetched data
                    if (!string.IsNullOrEmpty(authorData.Bio))
                        BioTextBox.Text = authorData.Bio;

                    if (authorData.BirthDate.HasValue)
                        BirthDatePicker.SelectedDate = authorData.BirthDate;

                    // Only update photo URL if it's currently empty
                    if (string.IsNullOrWhiteSpace(PhotoUrlTextBox.Text) && !string.IsNullOrEmpty(authorData.PhotoUrl))
                    {
                        PhotoUrlTextBox.Text = authorData.PhotoUrl;
                    }

                    HasOLKey = true;

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
                var birthDate = "";
                var photoUrl = "";

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

                // Get birth date
                if (!string.IsNullOrEmpty(json["birth_date"]?.ToString()))
                {
                    birthDate = DateNormalizer.NormalizeDate(json["birth_date"]?.ToString());
                }

                // Get photo
                if (json["photos"] is JArray photos && photos.Count > 0)
                {
                    var photoId = photos[0].ToString();
                    photoUrl = $"https://covers.openlibrary.org/a/id/{photoId}-M.jpg";
                }

                var authorInfo = new AuthorInfo
                {
                    Name = name,
                    Bio = bio,
                    PhotoUrl = photoUrl
                };

                if (!string.IsNullOrEmpty(birthDate) && DateTime.TryParse(birthDate, out var parsedDate))
                {
                    authorInfo.BirthDate = parsedDate;
                }

                return authorInfo;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsNameSimilar(string name1, string name2)
        {
            // Simple similarity check
            name1 = name1.ToLowerInvariant().Trim();
            name2 = name2.ToLowerInvariant().Trim();

            // Exact match
            if (name1 == name2) return true;

            // One contains the other
            if (name1.Contains(name2) || name2.Contains(name1)) return true;

            // Check if last names match
            var parts1 = name1.Split(' ');
            var parts2 = name2.Split(' ');

            if (parts1.Length > 0 && parts2.Length > 0)
            {
                if (parts1[^1] == parts2[^1])
                    return true;
            }

            return false;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var authorToUpdate = await LibraryService.GetAuthorByIdAsync(_authorId);
                if (authorToUpdate == null)
                {
                    MessageBox.Show("Author no longer exists.", "Error");
                    Close();
                    return;
                }

                // Update author with new values
                authorToUpdate.OpenLibraryKey = string.IsNullOrWhiteSpace(OLKeyTextBox.Text) ? null : OLKeyTextBox.Text.Trim();
                authorToUpdate.Bio = string.IsNullOrWhiteSpace(BioTextBox.Text) ? null : BioTextBox.Text.Trim();
                authorToUpdate.BirthDate = BirthDatePicker.SelectedDate;
                authorToUpdate.PhotoUrl = string.IsNullOrWhiteSpace(PhotoUrlTextBox.Text) ? null : PhotoUrlTextBox.Text.Trim();

                await LibraryService.UpdateAuthorAsync(authorToUpdate);

                MessageBox.Show("Author information updated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _changesMade = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving author: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_changesMade)
                DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _httpClient?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}