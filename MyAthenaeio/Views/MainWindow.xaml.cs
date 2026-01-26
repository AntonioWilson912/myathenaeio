using Microsoft.Win32;
using MyAthenaeio.Data;
using MyAthenaeio.Models.DTOs;
using MyAthenaeio.Models.Entities;
using MyAthenaeio.Models.ViewModels;
using MyAthenaeio.Scanner;
using MyAthenaeio.Services;
using MyAthenaeio.Utils;
using MyAthenaeio.Views.Books;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        private string? _searchText = null;
        private int? _selectedAuthorId = null;
        private int? _selectedGenreId = null;
        private int? _selectedTagId = null;
        private int? _selectedCollectionId = null;

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
                await LoadAuthorsFilter();
                await LoadGenresFilter();
                await LoadTagsFilter();
                await LoadCollectionsFilter();
                await SearchBooks();
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

        private async void ExportLibrary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"library_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var progressDialog = new ProgressDialog("Exporting Library", "Preparing export...") { Owner = this };
                    progressDialog.Show();

                    try
                    {
                        var export = await IMEXService.ExportToFileAsync(saveDialog.FileName);

                        progressDialog.Close();

                        MessageBox.Show($"Library exported successfully!\n\n" +
                                       $"Books: {export.Statistics.TotalBooks}\n" +
                                       $"Authors: {export.Statistics.TotalAuthors}\n" +
                                       $"Genres: {export.Statistics.TotalGenres}\n" +
                                       $"Tags: {export.Statistics.TotalTags}\n" +
                                       $"Collections: {export.Statistics.TotalCollections}\n" +
                                       $"Borrowers: {export.Statistics.TotalBorrowers}\n" +
                                       $"Book Copies: {export.Statistics.TotalCopies}\n" +
                                       $"Active Loans: {export.Statistics.ActiveLoans}\n" +
                                       $"Completed Loans: {export.Statistics.CompletedLoans}\n" +
                                       $"Renewal Records: {export.Statistics.TotalRenewals}\n\n" +
                                       $"File: {saveDialog.FileName}",
                            "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    finally
                    {
                        if (progressDialog.IsVisible)
                            progressDialog.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting library: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportLibrary_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Importing a library will merge data from the selected file into your existing library. " +
                "Duplicates will be avoided based on ISBNs and other unique fields.\n\n" +
                "Are you sure you want to proceed with importing a library?",
                "Confirm Import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    Multiselect = false
                };

                if (openDialog.ShowDialog() == true)
                {
                    var progressDialog = new ProgressDialog("Importing Library", "Preparing import...") { Owner = this };
                    progressDialog.Show();

                    try
                    {
                        var importResult = await IMEXService.ImportFromFileAsync(openDialog.FileName);
                        progressDialog.Close();

                        if (importResult.Success)
                        {
                            MessageBox.Show($"Import complete!\n\n" + 
                                            $"Books Added: {importResult.BooksImported}\n" +
                                            $"Authors Added: {importResult.AuthorsImported}\n" +
                                            $"Genres Added: {importResult.GenresImported}\n" +
                                            $"Tags Imported: {importResult.TagsImported}\n" +
                                            $"Collections Imported: {importResult.CollectionsImported}\n" +
                                            $"Borrowers Imported: {importResult.BorrowersImported}\n" +
                                            $"Book Copies Imported: {importResult.CopiesImported}\n" +
                                            $"Active Loans Imported: {importResult.LoansImported}\n" +
                                            $"Renewal Records Imported: {importResult.RenewalsImported}\n" +
                                            $"Items skipped (duplicates): {importResult.ItemsSkipped}",
                                "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Refresh the view
                            LoadBookData();
                        }
                        else
                        {
                            var errorMessage = "Import failed with the following errors:\n\n" +
                                string.Join("\n", importResult.Errors);
                            MessageBox.Show(errorMessage, "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    finally
                    {
                        if (progressDialog.IsVisible)
                            progressDialog.Close();
                    }
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Error parsing import file: {ex.Message}\n\nMake sure the file is a valid library export.",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing library: {ex.Message}",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RestoreFromBackup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RestoreDialog { Owner = this };
            dialog.ShowDialog();
        }

        private async void ResetDatabase_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ResetDialog { Owner = this };
            dialog.ShowDialog();
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
            var helpWindow = new HelpWindow() { Owner = this };
            helpWindow.Show();
        }

        private void ScannerHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow() { Owner = this };
            helpWindow.Show();
            helpWindow.SelectScannerHelpTab();
        }

        private void LoanHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow() { Owner = this };
            helpWindow.Show();
            helpWindow.SelectLoanHelpTab();
        }

        private void TroubleshootingHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow() { Owner = this };
            helpWindow.Show();
            helpWindow.SelectTroubleshootingTab();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            string versionString = $"{version!.Major}.{version.Minor}.{version.Build}";

            string copyrightText;
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);

            if (attributes.Length > 0 && attributes[0] is AssemblyCopyrightAttribute copyright)
            {
                copyrightText = copyright.Copyright;
            }
            else
            {
                copyrightText = $"© {DateTime.Now.Year} myAthenaeio Contributors";
            }

            MessageBox.Show(
                "myAthenaeio - Book Scanner & Library Manager\n\n" +
                $"Version {versionString}\n\n" +
                $"{copyrightText}",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    MessageBox.Show(
                        "Could not check for updates. Please try again later.",
                        "Update Check Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (updateInfo.IsUpdateAvailable == true)
                {
                    var result = MessageBox.Show(
                        "A new version is available!\n\n" +
                        $"Current: {updateInfo.CurrentVersion}\n" +
                        $"Latest: {updateInfo.LatestVersion}\n\n" +
                        "Download update?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = updateInfo.DownloadUrl,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"You're running the latest version ({updateInfo.CurrentVersion})!",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for updates");
            }
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

                bool wasSuccessful = bookResult.IsSuccess;
                string? errorMessage = bookResult.Error;
                BookApiResponse? book = bookResult.Value;

                if (!wasSuccessful)
                {
                    // Show user-friendly error
                    string friendlyError = BookApiService.GetUserFriendlyError(errorMessage ?? "Unknown error");

                    if (IsActive || sender == this)
                    {
                        MessageBox.Show(friendlyError, "Scan Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        _trayIconManager.ShowNotification("Scan Failed", friendlyError);
                    }
                }

                // Check if the book already exists in the DB
                Book? existingBookInDb = null;

                if (wasSuccessful && book != null)
                {
                    if (!string.IsNullOrEmpty(book.Isbn13))
                    {
                        existingBookInDb = await LibraryService.GetBookByISBNAsync(book.Isbn13);
                    }

                    if (existingBookInDb == null && !string.IsNullOrEmpty(book.Isbn10))
                    {
                        existingBookInDb = await LibraryService.GetBookByISBNAsync(book.Isbn10);
                    }

                    existingBookInDb ??= await LibraryService.GetBookByISBNAsync(barcode);
                }

                bool isInLibrary = existingBookInDb != null;
                int? bookId = existingBookInDb?.Id;

                // Add to log
                var newEntry = new ScanLogEntry
                {
                    Timestamp = DateTime.Now,
                    Barcode = ISBNValidator.FormatISBN(barcode),
                    Title = book?.Title ?? "Unknown",
                    Cover = book?.Cover ?? BookApiService.CreatePlaceholderImage(),
                    Source = sender == this ? "Manual" : (IsActive ? "Scanner" : "Background"),
                    IsCoverLoaded = true,
                    IsInLibrary = isInLibrary,
                    BookId = bookId,
                    WasSuccessful = wasSuccessful,
                    ErrorMessage = errorMessage
                };

                _scanLog.Insert(0, newEntry);

                // Update UI
                if (wasSuccessful)
                {
                    StatusText.Foreground = Brushes.Black;
                    StatusText.Text = $"Scanned: {ISBNValidator.FormatISBN(barcode)}";
                }
                else
                {
                    StatusText.Foreground = Brushes.Red;
                    StatusText.Text = $"Scan failed: {BookApiService.GetUserFriendlyError(errorMessage ?? "Unknown error")}";
                }

                ScanCountText.Text = _scanCount.ToString();
                _trayIconManager.UpdateTodayCount(_scanCount);
                ScannerInputField.Clear();

                if (!IsActive && sender != this)
                {
                    _trayIconManager.ShowNotification(
                        wasSuccessful ? "Book Scanned" : "Scan Failed",
                        wasSuccessful
                            ? $"ISBN: {ISBNValidator.FormatISBN(barcode)}"
                            : BookApiService.GetUserFriendlyError(errorMessage ?? "Unknown error"));
                }

                SaveScanData();
            });
        }

        #endregion

        #region Scan Log Handling

        private async void RetryScan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ScanLogEntry entry)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                string cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);

                // Try fetching again
                Result<BookApiResponse> bookResult = await BookApiService.FetchBookByISBN(cleanedBarcode);

                if (bookResult.IsSuccess)
                {
                    BookApiResponse book = bookResult.Value!;

                    // Update the entry
                    await Dispatcher.InvokeAsync(() =>
                    {
                        entry.WasSuccessful = true;
                        entry.ErrorMessage = null;
                        entry.Title = book.Title;
                        entry.Cover = book.Cover ?? BookApiService.CreatePlaceholderImage();
                        entry.IsCoverLoaded = true;

                        // Check if it's in library
                        _ = Task.Run(async () =>
                        {
                            var existingBook = await LibraryService.GetBookByISBNAsync(cleanedBarcode);
                            if (existingBook != null)
                            {
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    entry.IsInLibrary = true;
                                    entry.BookId = existingBook.Id;
                                });
                            }
                        });
                    });

                    StatusText.Foreground = Brushes.Green;
                    StatusText.Text = $"Successfully retrieved: {book.Title}";

                    SaveScanData();
                }
                else
                {
                    // Still failed
                    await Dispatcher.InvokeAsync(() =>
                    {
                        entry.ErrorMessage = bookResult.Error;
                    });

                    StatusText.Foreground = Brushes.Red;
                    StatusText.Text = $"Retry failed: {BookApiService.GetUserFriendlyError(bookResult.Error ?? "Unknown error")}";

                    MessageBox.Show(
                        BookApiService.GetUserFriendlyError(bookResult.Error ?? "Unknown error"),
                        "Retry Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during retry: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void AddManuallyFromScan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ScanLogEntry entry)
                return;

            try
            {
                // Pre-populate the ISBN in the add dialog
                var addWindow = new BookAddDialog(entry.Barcode) { Owner = this };

                if (addWindow.ShowDialog() == true)
                {
                    // Book was added successfully
                    var cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);
                    var addedBook = await LibraryService.GetBookByISBNAsync(cleanedBarcode);

                    if (addedBook != null)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            entry.WasSuccessful = true;
                            entry.ErrorMessage = null;
                            entry.Title = addedBook.Title;
                            entry.IsInLibrary = true;
                            entry.BookId = addedBook.Id;
                        });

                        StatusText.Foreground = Brushes.Green;
                        StatusText.Text = $"Manually added: {addedBook.Title}";

                        await SearchBooks();
                        SaveScanData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding book manually: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ScanLogEntry entry)
                return;

            // If already in library, show the book details instead
            if (entry.IsInLibrary && entry.BookId.HasValue)
            {
                try
                {
                    var detailWindow = new BookDetailWindow(entry.BookId.Value) { Owner = this };
                    detailWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening book details: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

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
                            scanEntry.WasSuccessful = true;
                            scanEntry.ErrorMessage = null;
                        }
                    }
                });

                StatusText.Foreground = Brushes.Green;
                StatusText.Text = $"Added {bookData.Title} to Library ✓";

                MessageBox.Show($"'{bookData.Title}' has been added to your library!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the library view
                await SearchBooks();

                // Save updated scan data
                SaveScanData();
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

                    SaveScanData(); // Save the updated state
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

                if (entry.WasSuccessful)
                {
                    // Fetch full book details for successful scans
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
                else
                {
                    // Show error details for failed scans
                    var detailWindow = new ScannedBookDetailWindow(entry.Barcode, entry.ErrorMessage ?? "Unknown error")
                    {
                        Owner = this
                    };
                    detailWindow.ShowDialog();
                }
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
                Source = entry.Source,
                WasSuccessful = entry.WasSuccessful,
                ErrorMessage = entry.ErrorMessage,
                IsInLibrary = entry.IsInLibrary,
                BookId = entry.BookId
            })]
                };

                string json = JsonSerializer.Serialize(dataToSave, SerializerOptions);
                File.WriteAllText(ScanSaveFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save scan data: {ex.Message}");
            }
        }

        private async void LoadBookData()
        {
            try
            {
                if (!File.Exists(AppDbContext.DbPath))
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
                Debug.WriteLine($"Failed to load book data: {ex.Message}");
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
                        IsCoverLoaded = false,
                        WasSuccessful = entry.WasSuccessful,
                        ErrorMessage = entry.ErrorMessage,
                        IsInLibrary = entry.IsInLibrary,
                        BookId = entry.BookId
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

                // Build lookup dictionary with all ISBN variants
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

                    // Also add the original ISBN as stored in the database
                    string cleanedDbIsbn = ISBNValidator.CleanISBN(book.ISBN);
                    if (!string.IsNullOrEmpty(cleanedDbIsbn))
                    {
                        booksByIsbn[cleanedDbIsbn] = (true, book.Id);
                    }
                }

                // Update all scan log entries
                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var entry in _scanLog)
                    {
                        string cleanedBarcode = ISBNValidator.CleanISBN(entry.Barcode);

                        // Try direct lookup first
                        if (booksByIsbn.TryGetValue(cleanedBarcode, out var bookInfo))
                        {
                            entry.IsInLibrary = bookInfo.IsInLibrary;
                            entry.BookId = bookInfo.BookId;
                        }
                        else
                        {
                            // Try both ISBN formats
                            var (isbn10, isbn13) = ISBNValidator.GetBothISBNFormats(cleanedBarcode);

                            if (!string.IsNullOrEmpty(isbn10) && booksByIsbn.TryGetValue(isbn10, out bookInfo))
                            {
                                entry.IsInLibrary = bookInfo.IsInLibrary;
                                entry.BookId = bookInfo.BookId;
                            }
                            else if (!string.IsNullOrEmpty(isbn13) && booksByIsbn.TryGetValue(isbn13, out bookInfo))
                            {
                                entry.IsInLibrary = bookInfo.IsInLibrary;
                                entry.BookId = bookInfo.BookId;
                            }
                            else
                            {
                                // Not in library
                                entry.IsInLibrary = false;
                                entry.BookId = null;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating scan history from DB: {ex.Message}");
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

        private async void AuthorsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AuthorsComboBox.SelectedItem is FilterOption selected)
            {
                _selectedAuthorId = selected.Value;
                await SearchBooks();
            }
        }

        private async void GenresComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GenresComboBox.SelectedItem is FilterOption selected)
            {
                _selectedGenreId = selected.Value;
                await SearchBooks();
            }
        }

        private async void TagsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagsComboBox.SelectedItem is FilterOption selected)
            {
                _selectedTagId = selected.Value;
                await SearchBooks();
            }
        }

        private async void CollectionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionsComboBox.SelectedItem is FilterOption selected)
            {
                _selectedCollectionId = selected.Value;
                await SearchBooks();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _searchText = string.IsNullOrWhiteSpace(textBox.Text) ? null : textBox.Text;
            }
        }

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SearchBooks();
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchBooks();
        }

        private async void AddManualEntryButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new BookAddDialog { Owner = this };

            if (addWindow.ShowDialog() == true)
            {
                await SearchBooks();
            }
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
            var detailWindow = new BookDetailWindow(book.Id) { Owner = this };

            if (detailWindow.ShowDialog() == true)
            {
                // Refresh the book list if changes were made
                await SearchBooks();
            }
        }

        private async Task SearchBooks()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var books = await LibraryService.SearchBooksAsync(
                    _searchText,
                    _selectedAuthorId,
                    _selectedGenreId,
                    _selectedTagId,
                    _selectedCollectionId,
                    BookIncludeOptions.Search);

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

        private async Task LoadAuthorsFilter()
        {
            try
            {
                var authors = await LibraryService.GetAllAuthorsAsync();

                var filterOptions = new List<FilterOption>
                {
                    new() { Display = "All Authors", Value = null}
                };

                filterOptions.AddRange(authors
                    .OrderBy(a => a.Name)
                    .Select(a => new FilterOption
                    {
                        Display = a.Name,
                        Value = a.Id
                    }));

                AuthorsComboBox.ItemsSource = filterOptions;
                AuthorsComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load authors filter: {ex.Message}");
            }
        }

        private async Task LoadGenresFilter()
        {
            try
            {
                var genres = await LibraryService.GetAllGenresAsync();

                var filterOptions = new List<FilterOption>
                {
                    new() { Display = "All Genres", Value = null}
                };

                filterOptions.AddRange(genres
                    .OrderBy(g => g.Name)
                    .Select(g => new FilterOption
                    {
                        Display = g.Name,
                        Value = g.Id
                    }));

                GenresComboBox.ItemsSource = filterOptions;
                GenresComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load genres filter: {ex.Message}");
            }
        }

        private async Task LoadTagsFilter()
        {
            try
            {
                var tags = await LibraryService.GetAllTagsAsync();

                var filterOptions = new List<FilterOption>
                {
                    new() { Display = "All Tags", Value = null}
                };

                filterOptions.AddRange(tags
                    .OrderBy(t => t.Name)
                    .Select(t => new FilterOption
                    {
                        Display = t.Name,
                        Value = t.Id
                    }));

                TagsComboBox.ItemsSource = filterOptions;
                TagsComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load tags filter: {ex.Message}");
            }
        }

        private async Task LoadCollectionsFilter()
        {
            try
            {
                var collections = await LibraryService.GetAllCollectionsAsync();

                var filterOptions = new List<FilterOption>
                {
                    new() { Display = "All Collections", Value = null}
                };

                filterOptions.AddRange(collections
                    .OrderBy(c => c.Name)
                    .Select(c => new FilterOption
                    {
                        Display = c.Name,
                        Value = c.Id
                    }));

                CollectionsComboBox.ItemsSource = filterOptions;
                CollectionsComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load collections filter: {ex.Message}");
            }
        }

        #endregion
    }

    public class FilterOption
    {
        public string Display { get; set; } = "";
        public int? Value { get; set; }  // null represents "All"
    }
}