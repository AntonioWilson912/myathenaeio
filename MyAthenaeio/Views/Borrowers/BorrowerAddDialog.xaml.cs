using MyAthenaeio.Models.Entities;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using System;
using System.Windows;

namespace MyAthenaeio.Views.Borrowers
{
    /// <summary>
    /// Interaction logic for BorrowerAddDialog.xaml
    /// </summary>
    public partial class BorrowerAddDialog : Window
    {
        public BorrowerAddDialog()
        {
            InitializeComponent();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
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

                // Validate and format email
                var email = EmailTextBox.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emailError = EmailValidationService.GetValidationError(email);
                    if (emailError != null)
                    {
                        MessageBox.Show(
                            $"Invalid email format:\n\n{emailError}",
                            "Invalid Email",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Check for duplicate
                    var existingBorrower = await LibraryService.GetBorrowerByEmailAsync(email);
                    if (existingBorrower != null)
                    {
                        MessageBox.Show(
                            "A borrower with this email already exists.",
                            "Duplicate Email",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                // Validate and format phone
                var phone = PhoneTextBox.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    var phoneError = PhoneValidationService.GetValidationError(phone);
                    if (phoneError != null)
                    {
                        MessageBox.Show(
                            $"Invalid phone number:\n\n{phoneError}",
                            "Invalid Phone",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Auto-format the phone number
                    var formatted = PhoneValidationService.FormatPhoneNumber(phone);
                    if (formatted != null)
                    {
                        phone = formatted;
                        PhoneTextBox.Text = formatted; // Update UI
                    }

                    // Check for duplicate (using normalized version)
                    var normalized = PhoneValidationService.NormalizePhoneNumber(phone);
                    if (normalized != null)
                    {
                        var existingBorrower = await LibraryService.GetBorrowerByPhoneAsync(normalized);
                        if (existingBorrower != null)
                        {
                            MessageBox.Show(
                                "A borrower with this phone number already exists.",
                                "Duplicate Phone",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                // Create new borrower
                var borrower = new Borrower
                {
                    Name = NameTextBox.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
                    DateAdded = DateTime.UtcNow
                };

                await LibraryService.AddBorrowerAsync(borrower);

                MessageBox.Show("Borrower added successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding borrower: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var phone = PhoneTextBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var formatted = PhoneValidationService.FormatPhoneNumber(phone);
                if (formatted != null)
                {
                    PhoneTextBox.Text = formatted;
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}