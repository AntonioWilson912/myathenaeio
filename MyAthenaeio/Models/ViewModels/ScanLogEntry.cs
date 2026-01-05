using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Models.ViewModels
{
    public class ScanLogEntry : INotifyPropertyChanged
    {
        private BitmapSource? _cover;
        private bool _isInLibrary;
        private int? _bookId;
        private bool _wasSuccessful;
        private string? _errorMessage;

        public DateTime Timestamp { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        public bool WasSuccessful
        {
            get => _wasSuccessful;
            set
            {
                if (_wasSuccessful != value)
                {
                    _wasSuccessful = value;
                    OnPropertyChanged(nameof(WasSuccessful));
                    OnPropertyChanged(nameof(ButtonText));
                    OnPropertyChanged(nameof(ShowAddButton));
                    OnPropertyChanged(nameof(ShowRetryButton));
                    OnPropertyChanged(nameof(ShowManualAddButton));
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                    OnPropertyChanged(nameof(UserFriendlyError));
                }
            }
        }

        [JsonIgnore]
        public string UserFriendlyError
        {
            get
            {
                if (string.IsNullOrEmpty(ErrorMessage))
                    return string.Empty;

                // Convert technical errors to user-friendly messages
                if (ErrorMessage.Contains("API returned 404") || ErrorMessage.Contains("NotFound"))
                    return "Book not found in online database";
                if (ErrorMessage.Contains("Network error") || ErrorMessage.Contains("timeout"))
                    return "Network connection issue - check your internet";
                if (ErrorMessage.Contains("Invalid JSON"))
                    return "Unable to read book data from server";
                if (ErrorMessage.Contains("missing title"))
                    return "Incomplete book information received";

                return "Unable to fetch book information";
            }
        }

        [JsonIgnore]
        public BitmapSource? Cover
        {
            get => _cover;
            set
            {
                if (_cover != value)
                {
                    _cover = value;
                    OnPropertyChanged(nameof(Cover));
                }
            }
        }

        [JsonIgnore]
        public bool IsCoverLoaded { get; set; } = false;

        [JsonIgnore]
        public bool IsInLibrary
        {
            get => _isInLibrary;
            set
            {
                if (_isInLibrary != value)
                {
                    _isInLibrary = value;
                    OnPropertyChanged(nameof(IsInLibrary));
                    OnPropertyChanged(nameof(ButtonText));
                    OnPropertyChanged(nameof(ShowAddButton));
                }
            }
        }

        [JsonIgnore]
        public int? BookId
        {
            get => _bookId;
            set
            {
                if (_bookId != value)
                {
                    _bookId = value;
                    OnPropertyChanged(nameof(BookId));
                }
            }
        }

        [JsonIgnore]
        public string ButtonText => IsInLibrary ? "In Library ✓" : "Add to Library";

        [JsonIgnore]
        public bool ShowAddButton => WasSuccessful;

        [JsonIgnore]
        public bool ButtonEnabled => !IsInLibrary;

        [JsonIgnore]
        public bool ShowRetryButton => !WasSuccessful;

        [JsonIgnore]
        public bool ShowManualAddButton => !WasSuccessful;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}