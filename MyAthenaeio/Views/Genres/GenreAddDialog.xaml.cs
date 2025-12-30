using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Genres
{
    public partial class GenreAddDialog : Window
    {
        private readonly List<Genre> _existingBookGenres;
        private List<Genre> _allGenres;
        public Genre? SelectedGenre { get; private set; }

        public GenreAddDialog(List<Genre> existingBookGenres)
        {
            InitializeComponent();
            _existingBookGenres = existingBookGenres;
            _allGenres = new List<Genre>();

            Loaded += async (s, e) => await LoadGenresAsync();
            NameTextBox.TextChanged += (s, e) => UpdateAddButtonState();
        }

        private async Task LoadGenresAsync()
        {
            _allGenres = await LibraryService.GetAllGenresAsync();
            _allGenres.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            FilterGenres();
        }

        private void FilterGenres(string? searchTerm = null)
        {
            var filtered = _allGenres.AsEnumerable();

            // Exclude genres already associated with the book
            filtered = filtered.Where(g => !_existingBookGenres.Any(eb => eb.Id == g.Id));

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                filtered = filtered.Where(g => g.Name.ToLower().Contains(lowerSearch));
            }

            ExistingGenresListBox.ItemsSource = filtered.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterGenres(SearchTextBox.Text);
        }

        private void ExistingGenresListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExistingGenresListBox.SelectedItem is Genre genre)
            {
                SelectGenre(genre);
            }
        }

        private void SelectGenre(Genre genre)
        {
            SelectedGenre = genre;
            DialogResult = true;
            Close();
        }

        private void UpdateAddButtonState()
        {
            AddButton.IsEnabled = !string.IsNullOrWhiteSpace(NameTextBox.Text);
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Genre name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Check if genre already exists (case-insensitive)
                var existingGenre = _allGenres.FirstOrDefault(g =>
                    g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existingGenre != null)
                {
                    // Genre already exists - just select it and add to book
                    if (_existingBookGenres.Any(g => g.Id == existingGenre.Id))
                    {
                        MessageBox.Show($"Genre '{existingGenre.Name}' is already associated with this book.",
                            "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    SelectGenre(existingGenre);
                    return;
                }

                // Create new genre
                var newGenre = await LibraryService.AddGenreAsync(name);
                SelectGenre(newGenre);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding genre: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}