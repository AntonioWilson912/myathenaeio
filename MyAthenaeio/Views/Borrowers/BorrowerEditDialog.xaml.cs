using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MyAthenaeio.Views.Borrowers
{
    public partial class BorrowerEditDialog : Window
    {
        private Borrower? _borrower;
        private readonly int _borrowerId;
        private bool _changesMade = false;

        public BorrowerEditDialog(int borrowerId)
        {
            InitializeComponent();
            _borrowerId = borrowerId;

            Loaded += async (s, e) => await LoadBorrowerData();

            Closing += (s, e) =>
            {
                if (DialogResult == null && _changesMade)
                {
                    DialogResult = true;
                }
            };
        }

        private async Task LoadBorrowerData()
        {
            _borrower = await LibraryService.GetBorrowerByIdAsync(_borrowerId);
            if (_borrower == null)
            {
                MessageBox.Show("Borrower not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            NameTextBox.Text = _borrower.Name;
            EmailTextBox.Text = _borrower.Email;
            PhoneTextBox.Text = _borrower.Phone;
            IsActiveCheckBox.IsChecked = _borrower.IsActive;
            NotesTextBox.Text = _borrower.Notes;

            // Show registration date
            if (_borrower.DateAdded != default)
            {
                RegisteredDateTextBlock.Text = $"Registered: {_borrower.DateAdded:yyyy-MM-dd}";
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Name is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate email if provided
                var email = EmailTextBox.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emailError = EmailValidationService.GetValidationError(email);
                    if (emailError != null)
                    {
                        MessageBox.Show($"Email validation failed:\n\n{emailError}\n\nPlease enter a valid email address or leave it blank.",
                            "Invalid Email", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Check for duplicate email (excluding current borrower)
                    var existingBorrower = await LibraryService.GetBorrowerByEmailAsync(email);
                    if (existingBorrower != null && existingBorrower.Id != _borrowerId)
                    {
                        MessageBox.Show("A borrower with this email already exists.", "Duplicate Email",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Get fresh copy from database
                var borrowerToUpdate = await LibraryService.GetBorrowerByIdAsync(_borrowerId);
                if (borrowerToUpdate == null)
                {
                    MessageBox.Show("Borrower not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update properties
                borrowerToUpdate.Name = NameTextBox.Text.Trim();
                borrowerToUpdate.Email = string.IsNullOrWhiteSpace(email) ? null : email;
                borrowerToUpdate.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
                borrowerToUpdate.IsActive = IsActiveCheckBox.IsChecked ?? true;
                borrowerToUpdate.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

                await LibraryService.UpdateBorrowerAsync(borrowerToUpdate);

                _changesMade = true;

                MessageBox.Show("Borrower updated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating borrower: {ex.Message}", "Error",
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