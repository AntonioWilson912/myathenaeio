using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Views.Genres
{
    /// <summary>
    /// Interaction logic for GenreManagementWindow.xaml
    /// </summary>
    public partial class GenreManagementWindow : Window
    {
        private readonly ObservableCollection<Genre> _genres;
        private bool _changesMade = false;

        public GenreManagementWindow()
        {
            InitializeComponent();
            _genres = [];
            GenresDataGrid.ItemsSource = _genres;

            Loaded += async (s, e) => await SearchGenresAsync();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task SearchGenresAsync()
        {
            try
            {
                // Get search term
                var searchTerm = SearchTextBox?.Text?.Trim();

                // Get all genres or search
                var genres = string.IsNullOrWhiteSpace(searchTerm)
                    ? await LibraryService.GetAllGenresAsync()
                    : await LibraryService.SearchGenresAsync(searchTerm);

                // Load book counts for all genres
                foreach (var genre in genres)
                {
                    genre.BookCount = await LibraryService.GetBookCountInGenreAsync(genre.Id);
                }

                // Apply book filter
                var bookFilter = BookFilterComboBox?.SelectedIndex ?? 0;
                if (bookFilter == 1) // With Books
                {
                    genres = [.. genres.Where(g => g.BookCount > 0)];
                }
                else if (bookFilter == 2) // Without Books
                {
                    genres = [.. genres.Where(g => g.BookCount == 0)];
                }

                // Sort by name
                genres = [.. genres.OrderBy(g => g.Name)];

                // Update collection
                _genres.Clear();
                foreach (var genre in genres)
                {
                    _genres.Add(genre);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching genres: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = SearchGenresAsync();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SearchGenresAsync();
        }

        private void BookFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                _ = SearchGenresAsync();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchGenresAsync();
        }

        private async void AddGenre_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GenreCreateDialog { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _changesMade = true;
                await SearchGenresAsync();
            }
        }

        private async void GenresDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GenresDataGrid.SelectedItem is Genre genre)
            {
                await OpenGenreDetails(genre);
            }
        }

        private async void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Genre genre)
            {
                await OpenGenreDetails(genre);
            }
        }

        private async Task OpenGenreDetails(Genre genre)
        {
            var detailWindow = new GenreDetailWindow(genre.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                _changesMade = true;
                await SearchGenresAsync();
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Genre genre)
            {
                var dialog = new GenreEditDialog(genre.Id) { Owner = this };

                if (dialog.ShowDialog() == true)
                {
                    _changesMade = true;
                    await SearchGenresAsync();
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Genre genre)
                return;

            // Check if genre has books
            if (genre.BookCount > 0)
            {
                MessageBox.Show(
                    $"Cannot delete genre '{genre.Name}' because it is associated with {genre.BookCount} book(s).\n\n" +
                    "Remove the genre from all books before deleting it.",
                    "Cannot Delete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the genre '{genre.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await LibraryService.DeleteGenreAsync(genre.Id);

                MessageBox.Show($"Genre '{genre.Name}' deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _changesMade = true;
                await SearchGenresAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting genre: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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