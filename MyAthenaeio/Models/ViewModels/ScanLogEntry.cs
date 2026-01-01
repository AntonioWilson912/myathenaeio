using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace MyAthenaeio.Models.ViewModels
{
    public class ScanLogEntry : INotifyPropertyChanged
    {
        private BitmapSource? _cover;
        private bool _isInLibary;
        private int? _bookId;

        public DateTime Timestamp { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

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
            get => _isInLibary;
            set
            {
                if (_isInLibary != value)
                {
                    _isInLibary = value;
                    OnPropertyChanged(nameof(IsInLibrary));
                    OnPropertyChanged(nameof(ButtonText));
                    OnPropertyChanged(nameof(ButtonEnabled));
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
        public bool ButtonEnabled => !IsInLibrary;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
