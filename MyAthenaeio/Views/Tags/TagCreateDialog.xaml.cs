using MyAthenaeio.Services;
using System.Windows;

namespace MyAthenaeio.Views.Tags
{
    /// <summary>
    /// Interaction logic for TagCreateDialog.xaml
    /// </summary>
    public partial class TagCreateDialog : Window
    {
        public TagCreateDialog()
        {
            InitializeComponent();
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Tag name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Check if already exists
                var existing = await LibraryService.GetTagByNameAsync(name);
                if (existing != null)
                {
                    MessageBox.Show($"Tag '{existing.Name}' already exists.",
                        "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LibraryService.AddTagAsync(name);

                MessageBox.Show("Tag created successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating tag: {ex.Message}",
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
