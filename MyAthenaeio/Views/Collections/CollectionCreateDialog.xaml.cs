using MyAthenaeio.Services;
using System.Windows;

namespace MyAthenaeio.Views.Collections
{
    /// <summary>
    /// Interaction logic for CollectionCreateDialog.xaml
    /// </summary>
    public partial class CollectionCreateDialog : Window
    {
        public CollectionCreateDialog()
        {
            InitializeComponent();
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Collection name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Check if already exists
                var existing = await LibraryService.GetCollectionByNameAsync(name);
                if (existing != null)
                {
                    MessageBox.Show($"Collection '{existing.Name}' already exists.",
                        "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var description = DescriptionTextBox.Text?.Trim();
                var notes = NotesTextBox.Text?.Trim();

                await LibraryService.AddCollectionAsync(name, description, notes);

                MessageBox.Show("Collection created successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating collection: {ex.Message}",
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
