using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using System.Windows;

namespace MyAthenaeio.Views.BookCopies
{
    /// <summary>
    /// Interaction logic for BookCopyEditDialog.xaml
    /// </summary>
    public partial class BookCopyEditDialog : Window
    {
        private readonly int _bookCopyId;
        private BookCopy? _bookCopy;
        private bool _changesMade = false;
        public BookCopyEditDialog(int bookCopyId)
        {
            InitializeComponent();
            _bookCopyId = bookCopyId;

            Loaded += async (s, e) => await LoadBookCopyData();
            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadBookCopyData()
        {
            _bookCopy = await LibraryService.GetBookCopyByIdAsync(_bookCopyId);

            if (_bookCopy == null)
            {
                MessageBox.Show("Book copy not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            CopyNumberTextBox.Text = _bookCopy.CopyNumber;
            AcquisitionDatePicker.SelectedDate = _bookCopy.AcquisitionDate;
            ConditionComboBox.SelectedItem = _bookCopy.Condition;
            NotesTextBox.Text = _bookCopy.Notes;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!AcquisitionDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Acquisition date is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var bookCopyToUpdate = await LibraryService.GetBookCopyByIdAsync(_bookCopyId);

                if (bookCopyToUpdate == null)
                {
                    MessageBox.Show("Book copy no longer exists.");
                    Close();
                    return;
                }

                bookCopyToUpdate.AcquisitionDate = AcquisitionDatePicker.SelectedDate.Value;
                bookCopyToUpdate.Condition = ConditionComboBox.SelectedItem?.ToString();
                bookCopyToUpdate.Notes = NotesTextBox.Text?.Trim();

                await LibraryService.UpdateBookCopyAsync(bookCopyToUpdate);

                MessageBox.Show("Book copy updated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _changesMade = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving book copy: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_changesMade)
                DialogResult = false;
            Close();
        }
    }
}