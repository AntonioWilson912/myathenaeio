using MyAthenaeio.Services;
using System.Windows;

namespace MyAthenaeio.Views.Genres
{
    /// <summary>
    /// Interaction logic for GenreCreateDialog.xaml
    /// </summary>
    public partial class GenreCreateDialog : Window
    {
        public GenreCreateDialog()
        {
            InitializeComponent();
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
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
                // Check if already exists
                var existing = await LibraryService.GetGenreByNameAsync(name);
                if (existing != null)
                {
                    MessageBox.Show($"Genre '{existing.Name}' already exists.",
                        "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LibraryService.AddGenreAsync(name);

                MessageBox.Show("Genre created successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating genre: {ex.Message}",
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
