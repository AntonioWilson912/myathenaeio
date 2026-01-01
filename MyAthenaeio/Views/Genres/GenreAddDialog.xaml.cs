using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Genres
{
    /// <summary>
    /// Interaction logic for GenreAddDialog.xaml
    /// </summary>
    public partial class GenreAddDialog : Window
    {
        private readonly int _bookId;
        private readonly ObservableCollection<Genre> _existingBookGenres;
        private readonly ObservableCollection<Genre> _allGenres;
        private readonly ObservableCollection<Genre> _filteredGenres;
        public Genre? SelectedGenre { get; private set; }

        public GenreAddDialog(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            _existingBookGenres = [];
            _allGenres = [];
            _filteredGenres = [];

            ExistingGenresListBox.ItemsSource = _filteredGenres;

            Loaded += async (s, e) => await LoadGenresAsync();
            NameTextBox.TextChanged += (s, e) => UpdateAddButtonState();
        }

        private async Task LoadGenresAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Load book's existing genres
                var book = await LibraryService.GetBookByIdAsync(_bookId, BookIncludeOptions.WithGenres);
                if (book == null)
                {
                    MessageBox.Show("Book not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                _existingBookGenres.Clear();
                foreach (var genre in book.Genres)
                {
                    _existingBookGenres.Add(genre);
                }

                // Load all genres
                var allGenres = await LibraryService.GetAllGenresAsync();
                _allGenres.Clear();
                foreach (var genre in allGenres.OrderBy(g => g.Name))
                {
                    _allGenres.Add(genre);
                }
                
                FilterGenres();
            } catch (Exception ex)
            {
                MessageBox.Show($"Error loading genres: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            } finally
            {
                Mouse.OverrideCursor = null;
            }
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

            _filteredGenres.Clear();

            foreach (var genre in filtered)
            {
                _filteredGenres.Add(genre);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterGenres(SearchTextBox.Text);
        }

        private void ExistingGenresListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddButton.IsEnabled = ExistingGenresListBox.SelectedItem != null;
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
            // Check if we're on "Existing Genres" tab and one is selected
            if (ExistingGenresListBox.SelectedItem is Genre selectedGenre)
            {
                SelectGenre(selectedGenre);
                return;
            }

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