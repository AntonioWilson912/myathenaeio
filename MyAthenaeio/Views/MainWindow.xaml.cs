using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Windows.Controls;
using MyAthenaeio.Scanner;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;

namespace MyAthenaeio.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ScannerManager _scannerManager;
        private readonly TrayIconManager _trayIconManager;
        private readonly ObservableCollection<ScanLogEntry> _scanLog;
        private int _scanCount = 0;

        private readonly HashSet<string> _loadingCovers = [];
        private readonly SemaphoreSlim _coverLoadSemaphore = new(3); // Limit concurrent cover loads

        private readonly ObservableCollection<Book> _books;
        private string _searchText = "";

        private static string LibrarySaveFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "myAthenaeio",
            "library.db"
        );

        private static string ScanSaveFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "myAthenaeio",
            "scan_data.json"
        );

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        public MainWindow()
        {
            InitializeComponent();

            _scanLog = [];
            ScanLogList.ItemsSource = _scanLog;

            _books = [];

            _scannerManager = new ScannerManager();
            _scannerManager.BarcodeScanned += OnBarcodeScanned;

            // Initialize system tray
            _trayIconManager = new TrayIconManager(_scannerManager);

            // Set initial mode when window loads
            Loaded += (s, e) =>
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                UpdateCurrentModeText();
                _ = LoadBooks();
            };

            // Handle minimize to tray behavior
            StateChanged += Window_StateChanged;

            // Load saved book data
            LoadBookData();

            // Load saved scan data
            LoadScanData();

            // Register closing event
            Closing += Window_Closing;
        }

        private void ScannerInputField_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Handle manual input
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                string text = ScannerInputField.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    ProcessManualInput(text);
                    e.Handled = true;
                }
            }
            else
            {
                // Pass key to scanner manager
                _scannerManager.ProcessKey(e.Key);
            }
        }

        private void ProcessManualInput(string input)
        {
            // Validate ISBN
            if (ISBNValidator.IsValidISBNFormat(input))
            {
                // Clean and process
                string cleaned = ISBNValidator.CleanISBN(input);
                OnBarcodeScanned(this, cleaned);
            }
            else
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Text = $"❌ Invalid ISBN format: {input}";
            }
        }


        private void OnBarcodeScanned(object? sender, string barcode)
        {
            if (sender == null) return;

            Dispatcher.Invoke(async () =>
            {
                _scanCount++;

                // Fetch ISBN details
                Result<BookApiResponse> bookResult = await BookAPIService.FetchBookByISBN(barcode);
                if (!bookResult.IsSuccess)
                {
                    //Only show error if window is active or this is a manual scan
                    if (IsActive || sender == this)
                    {
                        MessageBox.Show(bookResult.Error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        // Show tray notification for background errors
                        _trayIconManager.ShowNotification(
                            "Scan Failed",
                            $"Failed to fetch book: {barcode}");
                    }
                    return;
                }

                BookApiResponse book = bookResult.Value!;

                // Check if the book already exists in the DB
                Book? existingBookInDb = null;

                if (!string.IsNullOrEmpty(book.Isbn13))
                {
                    existingBookInDb = await LibraryService.GetBookByISBNAsync(book.Isbn13);
                }

                if (existingBookInDb == null && !string.IsNullOrEmpty(book.Isbn10))
                {
                    existingBookInDb = await LibraryService.GetBookByISBNAsync(book.Isbn10);
                }

                // Fallback: check using the scanned barcode itself
                existingBookInDb ??= await LibraryService.GetBookByISBNAsync(barcode);

                bool isInLibrary = existingBookInDb != null;
                int? bookId = existingBookInDb?.Id;

                // Add to log
                var newEntry = new ScanLogEntry
                {
                    Timestamp = DateTime.Now,
                    Barcode = ISBNValidator.FormatISBN(barcode),
                    Title = book.Title,
                    Cover = book.Cover ?? BookAPIService.CreatePlaceholderImage(),
                    Source = sender == this ? "Manual" : (IsActive ? "Scanner" : "Background"),
                    IsCoverLoaded = true,
                    IsInLibrary = isInLibrary,
                    BookId = bookId
                };

                _scanLog.Insert(0, newEntry);

                // Update UI
                StatusText.Foreground = Brushes.Black;
                StatusText.Text = $"Scanned: {ISBNValidator.FormatISBN(barcode)}";
                ScanCountText.Text = _scanCount.ToString();

                // Update tray icon count
                _trayIconManager.UpdateTodayCount(_scanCount);

                // Clear input field
                ScannerInputField.Clear();

                // Show notification if app is not focused
                if (!IsActive && sender != this)
                    _trayIconManager.ShowNotification(
                        "Book Scanned",
                        $"ISBN: {ISBNValidator.FormatISBN(barcode)}"
                    );

                SaveScanData();
            });
        }

        private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ScanLogEntry entry)
                return;

            if (entry.IsInLibrary)
                return;

            Mouse.OverrideCursor = Cursors.Wait;

            string barcodeToAdd = entry.Barcode;

            try
            {
                // Fetch full book details
                string cleanedBarcode = ISBNValidator.CleanISBN(barcodeToAdd);
                Result<BookApiResponse> bookResult = await BookAPIService.FetchFullBookByISBN(cleanedBarcode);

                if (!bookResult.IsSuccess)
                {
                    MessageBox.Show($"Could not load book details: {bookResult.Error}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                BookApiResponse bookData = bookResult.Value!;

                // Create Book entity
                var book = new Book
                {
                    ISBN = bookData.Isbn13 ?? bookData.Isbn10 ?? entry.Barcode,
                    Title = bookData.Title,
                    Subtitle = bookData.Subtitle,
                    Description = bookData.Description,
                    Publisher = bookData.Publisher,
                    PublicationYear = bookData.PublishDate?.Year,
                    CoverImageUrl = bookData.CoverImageUrl,
                    Copies = []
                };

                var addedBook = await LibraryService.AddBookAsync(book, bookData.Authors);

                // Update all entries with matching ISBN
                var (convertedISBN10, convertedISBN13) = ISBNValidator.GetBothISBNFormats(barcodeToAdd);

                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var scanEntry in _scanLog)
                    {
                        var (entryISBN10, entryISBN13) = ISBNValidator.GetBothISBNFormats(scanEntry.Barcode);

                        // Check if any format matches
                        bool matches = false;
                        if (!string.IsNullOrEmpty(entryISBN10) && !string.IsNullOrEmpty(convertedISBN10))
                            matches |= entryISBN10 == convertedISBN10;
                        if (!string.IsNullOrEmpty(entryISBN13) && !string.IsNullOrEmpty(convertedISBN13))
                            matches |= entryISBN13 == convertedISBN13;

                        if (matches)
                        {
                            scanEntry.IsInLibrary = true;
                            scanEntry.BookId = addedBook.Id;
                        }
                    }
                });

                StatusText.Foreground = Brushes.Green;
                StatusText.Text = $"Added {bookData.Title} to Library ✓";

                MessageBox.Show($"'{bookData.Title}' has been added to your library!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the library view
                await LoadBooks();
            }
            catch (InvalidOperationException ex)
            {
                string cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);
                var existingBook = await LibraryService.GetBookByISBNAsync(cleanedBarcode);

                if (existingBook != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var scanEntry in _scanLog)
                        {
                            string cleanedScanBarcode = ISBNValidator.CleanISBN(scanEntry.Barcode);
                            string cleanedExistingISBN = ISBNValidator.CleanISBN(existingBook.ISBN);

                            if (cleanedScanBarcode == cleanedExistingISBN)
                            {
                                scanEntry.IsInLibrary = true;
                                scanEntry.BookId = existingBook.Id;
                            }
                        }
                    });
                }
                else
                {
                    entry.IsInLibrary = true;
                }

                entry.IsInLibrary = true;
                MessageBox.Show(ex.Message, "Already in Library",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add book: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (sender == null) return;

            if (WindowState == WindowState.Minimized)
            {
                // User wants background scanning
                if (BackgroundScanningCheckbox.IsChecked == true)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);

                    // Check if user actually approved it (might have said no to dialog)
                    if (_scannerManager.BackgroundModeEnabled)
                    {
                        StatusText.Foreground = Brushes.Black;
                        StatusText.Text = "📚 Scanner active in background";
                        _trayIconManager.ShowNotification(
                            "myAthenaeio",
                            "Scanner is active in background");
                    }
                    else
                    {
                        // User declined the permission - uncheck the box
                        BackgroundScanningCheckbox.IsChecked = false;
                        StatusText.Foreground = Brushes.Black;
                        StatusText.Text = "Scanner disabled (minimized)";
                    }
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                    StatusText.Foreground = Brushes.Black;
                    StatusText.Text = "Scanner disabled (minimized)";
                }
            }
            else
            {
                // Window is normal or maximized - use focused mode
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                StatusText.Foreground = Brushes.Black;
                StatusText.Text = "Ready to scan";
            }

            UpdateCurrentModeText();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                UpdateCurrentModeText();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Only handle background mode if minimized
            // Don't trigger when just clicking away from the window
            if (WindowState == WindowState.Minimized)
            {
                if (BackgroundScanningCheckbox.IsChecked == true && _scannerManager.BackgroundModeEnabled)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                }
                UpdateCurrentModeText();
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save books for later use
            SaveScanData();

            _trayIconManager?.Dispose();
            _scannerManager?.Dispose();
        }

        private void BackgroundScanning_Changed(object sender, RoutedEventArgs e)
        {
            if (_scannerManager == null) return;

            // Only do something if the window is currently minimized or inactive
            if (WindowState == WindowState.Minimized)
            {
                if (BackgroundScanningCheckbox.IsChecked == true)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);

                    // If user declined permission, uncheck
                    if (!_scannerManager.BackgroundModeEnabled)
                    {
                        BackgroundScanningCheckbox.IsChecked = false;
                    }
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                }

                UpdateCurrentModeText();
            }
            // If window is visible, just update the text - mode will change on minimize
        }

        private void UpdateCurrentModeText()
        {
            string modeText = WindowState == WindowState.Minimized
                ? (BackgroundScanningCheckbox.IsChecked == true && _scannerManager.BackgroundModeEnabled
                    ? "Background Service (Active)"
                    : "Disabled (Minimized)")
                : (IsActive ? "Focused Field Only" : "Disabled");

            CurrentModeText.Text = modeText;
        }

        private void SaveScanData()
        {
            try
            {
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(ScanSaveFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var dataToSave = new AppData
                {
                    ScanCount = _scanCount,
                    ScanLog = [.. _scanLog.Select(entry => new ScanLogEntry
                    {
                        Timestamp = entry.Timestamp,
                        Barcode = entry.Barcode,
                        Title = entry.Title,
                        Source = entry.Source
                    })]
                };

                string json = JsonSerializer.Serialize(dataToSave, SerializerOptions);

                File.WriteAllText(ScanSaveFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save scan data: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void LoadBookData()
        {
            try
            {
                if (!File.Exists(LibrarySaveFilePath))
                    return; // No saved data yet

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load book data: {ex.Message}", "Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadScanData()
        {
            try
            {
                if (!File.Exists(ScanSaveFilePath))
                    return; // No saved data yet

                string json = File.ReadAllText(ScanSaveFilePath);
                var loadedData = JsonSerializer.Deserialize<AppData>(json);

                if (loadedData == null)
                    return;

                _scanCount = loadedData.ScanCount;

                _scanLog.Clear();
                foreach (var entry in loadedData.ScanLog)
                {
                    _scanLog.Add(new ScanLogEntry
                    {
                        Timestamp = entry.Timestamp,
                        Barcode = entry.Barcode,
                        Title = entry.Title,
                        Source = entry.Source,
                        Cover = BookAPIService.CreatePlaceholderImage(),
                        IsCoverLoaded = false
                    });
                }

                // Update UI
                ScanCountText.Text = _scanCount.ToString();
                _trayIconManager.UpdateTodayCount(_scanCount);

                // Load covers for initially visible items
                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(100); // Let UI render
                    await UpdateScanHistoryFromDatabase(); // Check DB status
                    await LoadVisibleCovers();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task UpdateScanHistoryFromDatabase()
        {
            try
            {
                // Get all books from database
                var allBooks = await LibraryService.GetAllBooksAsync();

                // Create a dictionary for fast lookup by cleaned ISBN (both formats)
                var booksByIsbn = new Dictionary<string, (bool IsInLibrary, int BookId)>();

                foreach (var book in allBooks)
                {
                    var (isbn10, isbn13) = ISBNValidator.GetBothISBNFormats(book.ISBN);

                    // Add both ISBN formats to dictionary
                    if (!string.IsNullOrEmpty(isbn10))
                    {
                        booksByIsbn[isbn10] = (true, book.Id);
                    }
                    if (!string.IsNullOrEmpty(isbn13))
                    {
                        booksByIsbn[isbn13] = (true, book.Id);
                    }
                }

                // Update all scan entries
                foreach (var entry in _scanLog)
                {
                    string cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);

                    if (booksByIsbn.TryGetValue(cleanedBarcode, out var bookInfo))
                    {
                        entry.IsInLibrary = bookInfo.IsInLibrary;
                        entry.BookId = bookInfo.BookId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating scan history from DB: {ex.Message}");
            }
        }

        private async void ScanLogList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Load covers when user scrolls
            await LoadVisibleCovers();
        }

        private async void ScanLogList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ScanLogList.SelectedItem is not ScanLogEntry entry)
                return;

            // Show loading cursor
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // Fetch full book details
                string cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);
                Result<BookApiResponse> bookResult = await BookAPIService.FetchFullBookByISBN(cleanedBarcode);

                if (!bookResult.IsSuccess)
                {
                    MessageBox.Show($"Could not load book details: {bookResult.Error}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Open detail window
                var detailWindow = new BookDetailWindow(bookResult.Value!)
                {
                    Owner = this
                };
                detailWindow.ShowDialog();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadVisibleCovers()
        {
            try
            {
                var visibleItems = GetVisibleItems();

                foreach (var entry in visibleItems)
                {
                    if (!entry.IsCoverLoaded && !_loadingCovers.Contains(entry.Barcode))
                    {
                        _loadingCovers.Add(entry.Barcode);

                        // Load cover in background without blocking UI
                        await LoadCoverForEntry(entry);
                    }
                }
            }
            catch { }
        }

        private async Task LoadCoverForEntry(ScanLogEntry entry)
        {
            await _coverLoadSemaphore.WaitAsync();

            try
            {
                string cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);
                var coverResult = await BookAPIService.FetchCoverByISBN(cleanedBarcode);

                if (coverResult.IsSuccess)
                {
                    // Update on UI thread
                    await Dispatcher.InvokeAsync(() =>
                    {
                        entry.Cover = coverResult.Value;
                        entry.IsCoverLoaded = true;
                    });
                }
                else
                {
                    entry.IsCoverLoaded = true; // Mark as loaded even if failed
                }
            }
            catch
            {
                entry.IsCoverLoaded = true; // Prevent retrying on failure
            }
            finally
            {
                _loadingCovers.Remove(entry.Barcode);
                _coverLoadSemaphore.Release();
            }
        }

        private List<ScanLogEntry> GetVisibleItems()
        {
            var visibleItems = new List<ScanLogEntry>();

            if (ScanLogList.Items.Count == 0)
                return visibleItems;

            // Get the container for the ListView
            var scrollViewer = FindScrollViewer(ScanLogList);
            if (scrollViewer == null)
                return visibleItems;

            // Calculate visible range
            int firstVisible = (int)(scrollViewer.VerticalOffset);
            int lastVisible = (int)(scrollViewer.VerticalOffset + scrollViewer.ViewportHeight);

            // Add buffer for smooth scrolling
            firstVisible = Math.Max(0, firstVisible - 5);
            lastVisible = Math.Min(ScanLogList.Items.Count - 1, lastVisible + 5);

            for (int i = firstVisible; i <= lastVisible; i++)
            {
                if (i < _scanLog.Count)
                {
                    visibleItems.Add(_scanLog[i]);
                }
            }

            return visibleItems;
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject root)
        {
            if (root is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _coverLoadSemaphore?.Dispose();
        }

        // Event handlers for View Library tab
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _searchText = textBox.Text;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchBooks();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBooks();
        }

        private async void AddManualEntryButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new BookAddWindow
            {
                Owner = this
            };
            if (addWindow.ShowDialog() == true)
            {
                // Refresh the library after adding
                await LoadBooks();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadBooks();
        }

        private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is Book book)
            {
                await ViewBookDetails(book);
            }
        }

        private async void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Book book)
            {
                await ViewBookDetails(book);
            }
        }

        private async Task ViewBookDetails(Book book)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // Fetch full book details
                string cleanedISBN = ISBNValidator.CleanISBN(book.ISBN);
                Result<BookApiResponse> bookResult = await BookAPIService.FetchFullBookByISBN(cleanedISBN);

                if (!bookResult.IsSuccess)
                {
                    MessageBox.Show($"Could not load book details: {bookResult.Error}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Open detail window
                var detailWindow = new BookDetailWindow(bookResult.Value!)
                {
                    Owner = this
                };
                detailWindow.ShowDialog();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void SearchBooks()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    await LoadBooks();
                    return;
                }

                var books = await LibraryService.SearchBooksAsync(_searchText);
                _books.Clear();
                foreach (var book in books)
                {
                    _books.Add(book);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to search books: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadBooks()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                var books = await LibraryService.GetAllBooksAsync();
                _books.Clear();
                foreach (var book in books)
                {
                    _books.Add(book);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load books: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}