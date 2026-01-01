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
using MyAthenaeio.Views.Books;
using System.Diagnostics;

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
        internal readonly SettingsService _settingsService;
        private int _scanCount = 0;

        private readonly HashSet<string> _loadingCovers = [];
        private readonly SemaphoreSlim _coverLoadSemaphore = new(3);

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
            BooksDataGrid.ItemsSource = _books;

            _settingsService = new SettingsService();
            _scannerManager = new ScannerManager(_settingsService);
            _scannerManager.BarcodeScanned += OnBarcodeScanned;

            // Initialize system tray
            _trayIconManager = new TrayIconManager(_scannerManager);

            // Set initial mode when window loads
            Loaded += async (s, e) =>
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
                await LoadBooks();
            };

            // Handle window state changes
            StateChanged += Window_StateChanged;
            Activated += Window_Activated;
            Deactivated += Window_Deactivated;
            Closing += Window_Closing;

            // Load saved data
            LoadBookData();
            LoadScanData();
        }

        #region Menu Event Handlers

        private void ExportLibrary_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Library export funcitonality coming soon!", "That Is Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ImportLibrary_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Library import funcitonality coming soon!", "That Is Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            SaveScanData();
            Close();
        }

        private void ManageBorrowers_Click(object sender, RoutedEventArgs e)
        {
            var window = new Borrowers.BorrowerManagementWindow { Owner = this };
            window.ShowDialog();
        }

        private void ManageGenres_Click(object sender, RoutedEventArgs e)
        {
            var window = new Genres.GenreManagementWindow { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadBookData();
            }
        }

        private void ManageTags_Click(object sender, RoutedEventArgs e)
        {
            var window = new Tags.TagManagementWindow { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadBookData();
            }
        }

        private void ManageCollections_Click(object sender, RoutedEventArgs e)
        {
            var window = new Collections.CollectionManagementWindow { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadBookData();
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsDialog(_settingsService) { Owner = this };

            if (settingsDialog.ShowDialog() == true)
            {
                // Settings were saved, scanner manager will handle mode changes
                // based on current window state
                if (WindowState == WindowState.Minimized &&
                    _settingsService.Settings.BackgroundScanningEnabled)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                }
            }
        }

        private void ViewHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Help documentation coming soon!", "Help",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "myAthenaeio - Book Scanner & Library Manager\n\n" +
                "Version 1.0\n\n" +
                "© 2025",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region Scanner Input Handling

        private void ScannerInputField_PreviewKeyDown(object sender, KeyEventArgs e)
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

        private void ProcessISBNButton_Click(object sender, RoutedEventArgs e)
        {
            string text = ScannerInputField.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                ProcessManualInput(text);
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
                Result<BookApiResponse> bookResult = await BookApiService.FetchBookByISBN(barcode);
                if (!bookResult.IsSuccess)
                {
                    // Only show error if window is active or this is a manual scan
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
                    Cover = book.Cover ?? BookApiService.CreatePlaceholderImage(),
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
                {
                    _trayIconManager.ShowNotification(
                        "Book Scanned",
                        $"ISBN: {ISBNValidator.FormatISBN(barcode)}");
                }

                SaveScanData();
            });
        }

        #endregion

        #region Scan Log Handling

        private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ScanLogEntry entry)
                return;

            if (entry.IsInLibrary)
                return;

            string barcodeToAdd = entry.Barcode;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Fetch full book details
                string cleanedBarcode = ISBNValidator.CleanISBN(barcodeToAdd);
                Result<BookApiResponse> bookResult = await BookApiService.FetchFullBookByISBN(cleanedBarcode);

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

                MessageBox.Show(ex.Message, "Already in Library",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add book: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void ScanLogList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ScanLogList.SelectedItem is not ScanLogEntry entry)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Fetch full book details
                string cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);
                Result<BookApiResponse> bookResult = await BookApiService.FetchFullBookByISBN(cleanedBarcode);

                if (!bookResult.IsSuccess)
                {
                    MessageBox.Show($"Could not load book details: {bookResult.Error}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Open detail window
                var detailWindow = new ScannedBookDetailWindow(bookResult.Value!) { Owner = this };
                detailWindow.ShowDialog();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void ScanLogList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Load covers when user scrolls
            await LoadVisibleCovers();
        }

        #endregion

        #region Window State Management

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            // Handle restoration from hidden state
            if (WindowState != WindowState.Minimized && !this.IsVisible)
            {
                this.Show();
                this.ShowInTaskbar = true;
            }
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (sender == null) return;

            try
            {
                if (WindowState == WindowState.Minimized)
                {
                    if (_settingsService.Settings.BackgroundScanningEnabled)
                    {
                        Hide();
                        ShowInTaskbar = false;

                        _scannerManager.SetMode(ScannerMode.BackgroundService);

                        if (_scannerManager.BackgroundModeEnabled)
                        {
                            _trayIconManager.ShowNotification(
                                "myAthenaeio",
                                "Scanner is active in background");
                        }
                        else
                        {
                            _settingsService.Settings.BackgroundScanningEnabled = false;
                            _settingsService.SaveSettings();
                        }
                    }
                    else
                    {
                        _scannerManager.SetMode(ScannerMode.Disabled);
                    }
                }
                else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
                {
                    Show();
                    ShowInTaskbar = true;

                    _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                    ScannerInputField.Focus();
                    StatusText.Foreground = Brushes.Black;
                    StatusText.Text = "Ready to scan";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Window_StateChanged: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Ensure window is visible on error
                Show();
                ShowInTaskbar = true;
                WindowState = WindowState.Normal;
            }
        }

        private void Window_Activated(object? sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                _scannerManager.SetMode(ScannerMode.FocusedFieldOnly);
                ScannerInputField.Focus();
            }
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            // Only handle background mode if minimized
            if (WindowState == WindowState.Minimized)
            {
                if (_settingsService.Settings.BackgroundScanningEnabled &&
                    _scannerManager.BackgroundModeEnabled)
                {
                    _scannerManager.SetMode(ScannerMode.BackgroundService);
                }
                else
                {
                    _scannerManager.SetMode(ScannerMode.Disabled);
                }
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveScanData();
            _settingsService.SaveSettings();
            _trayIconManager?.Dispose();
            _scannerManager?.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _coverLoadSemaphore?.Dispose();
        }

        #endregion

        #region Data Persistence

        private void SaveScanData()
        {
            try
            {
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
                System.Diagnostics.Debug.WriteLine($"Failed to save scan data: {ex.Message}");
            }
        }

        private async void LoadBookData()
        {
            try
            {
                if (!File.Exists(LibrarySaveFilePath))
                    return;

                var books = await LibraryService.GetAllBooksAsync(BookIncludeOptions.Search);

                await Dispatcher.InvokeAsync(() =>
                {
                    _books.Clear();
                    foreach (var book in books)
                    {
                        _books.Add(book);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load book data: {ex.Message}");
            }
        }

        private void LoadScanData()
        {
            try
            {
                if (!File.Exists(ScanSaveFilePath))
                    return;

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
                        Cover = BookApiService.CreatePlaceholderImage(),
                        IsCoverLoaded = false
                    });
                }

                // Update UI
                ScanCountText.Text = _scanCount.ToString();
                _trayIconManager.UpdateTodayCount(_scanCount);

                // Load covers for initially visible items
                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(100);
                    await UpdateScanHistoryFromDatabase();
                    await LoadVisibleCovers();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load scan data: {ex.Message}");
            }
        }

        private async Task UpdateScanHistoryFromDatabase()
        {
            try
            {
                var allBooks = await LibraryService.GetAllBooksAsync();
                var booksByIsbn = new Dictionary<string, (bool IsInLibrary, int BookId)>();

                foreach (var book in allBooks)
                {
                    var (isbn10, isbn13) = ISBNValidator.GetBothISBNFormats(book.ISBN);

                    if (!string.IsNullOrEmpty(isbn10))
                    {
                        booksByIsbn[isbn10] = (true, book.Id);
                    }
                    if (!string.IsNullOrEmpty(isbn13))
                    {
                        booksByIsbn[isbn13] = (true, book.Id);
                    }
                }

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

        #endregion

        #region Cover Loading

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
                var coverResult = await BookApiService.FetchCoverByISBN(cleanedBarcode);

                if (coverResult.IsSuccess)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        entry.Cover = coverResult.Value;
                        entry.IsCoverLoaded = true;
                    });
                }
                else
                {
                    entry.IsCoverLoaded = true;
                }
            }
            catch
            {
                entry.IsCoverLoaded = true;
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

            var scrollViewer = FindScrollViewer(ScanLogList);
            if (scrollViewer == null)
                return visibleItems;

            int firstVisible = (int)(scrollViewer.VerticalOffset);
            int lastVisible = (int)(scrollViewer.VerticalOffset + scrollViewer.ViewportHeight);

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

        #endregion

        #region Library Tab Event Handlers

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
            var addWindow = new BookAddDialog { Owner = this };

            if (addWindow.ShowDialog() == true)
            {
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

            var detailWindow = new BookDetailWindow(book.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                // Refresh the book list if changes were made
                await LoadBooks();
            }
        }

        private async void SearchBooks()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    await LoadBooks();
                    return;
                }

                var books = await LibraryService.SearchBooksAsync(_searchText, BookIncludeOptions.Search);

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
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var books = await LibraryService.GetAllBooksAsync(BookIncludeOptions.Search);

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

        #endregion
    }
}