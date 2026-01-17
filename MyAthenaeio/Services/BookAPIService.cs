using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using MyAthenaeio.Utils;
using MyAthenaeio.Models.DTOs;
using Serilog;

namespace MyAthenaeio.Services
{
    internal static class BookApiService
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(BookApiService));
        private static readonly HttpClient _bookClient = new();
        private const string _bookISBNUrlTemplate = "https://openlibrary.org/isbn/{0}.json";
        private const string _coverUrlTemplate = "https://covers.openlibrary.org/b/isbn/{0}-M.jpg";
        private const string _authorURLTemplate = "https://openlibrary.org/authors/{0}.json";
        private const string _workURLTemplate = "https://openlibrary.org/works/{0}.json";
        private static BitmapSource? _placeholderImage;

        private static readonly Dictionary<string, BitmapSource> _coverCache = [];

        public static async Task<Result<BookApiResponse>> FetchBookByISBN(string isbn)
        {
            try
            {
                _logger.Debug("Fetching book by ISBN: {ISBN}", isbn);

                var url = string.Format(_bookISBNUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warning("API returned {StatusCode} for ISBN: {ISBN}", response.StatusCode, isbn);
                    return Result<BookApiResponse>.Failure($"API returned {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                JObject parsedJson = JObject.Parse(jsonResponse);

                string? title = parsedJson["title"]?.ToString();
                if (string.IsNullOrEmpty(title))
                {
                    _logger.Warning("Book data missing title for ISBN: {ISBN}", isbn);
                    return Result<BookApiResponse>.Failure("Book data missing title");
                }

                string? isbn10 = parsedJson["isbn_10"]?[0]?.ToString();
                string? isbn13 = parsedJson["isbn_13"]?[0]?.ToString();

                if (string.IsNullOrEmpty(isbn10) && string.IsNullOrEmpty(isbn13))
                {
                    string cleanedIsbn = ISBNValidator.CleanISBN(isbn);
                    if (cleanedIsbn.Length == 10)
                        isbn10 = cleanedIsbn;
                    else if (cleanedIsbn.Length == 13)
                        isbn13 = cleanedIsbn;
                }

                BitmapSource? cover = null;
                var coverResult = await FetchCoverByISBN(isbn);
                if (coverResult.IsSuccess)
                {
                    cover = coverResult.Value;
                }

                BookApiResponse book = new()
                {
                    Title = title,
                    Isbn10 = isbn10,
                    Isbn13 = isbn13,
                    Cover = cover ?? CreatePlaceholderImage()
                };

                _logger.Information("Successfully fetched book: {Title} (ISBN: {ISBN})", title, isbn);
                return Result<BookApiResponse>.Success(book);

            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex, "Network error fetching ISBN: {ISBN}", isbn);
                return Result<BookApiResponse>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.Error(ex, "Invalid JSON for ISBN: {ISBN}", isbn);
                return Result<BookApiResponse>.Failure($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error fetching ISBN: {ISBN}", isbn);
                return Result<BookApiResponse>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        public static async Task<Result<BookApiResponse>> FetchFullBookByISBN(string isbn)
        {
            try
            {
                _logger.Debug("Fetching full book data by ISBN: {ISBN}", isbn);

                var url = string.Format(_bookISBNUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warning("API returned {StatusCode} for full book ISBN: {ISBN}", response.StatusCode, isbn);
                    return Result<BookApiResponse>.Failure($"API returned {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                JObject parsedJson = JObject.Parse(jsonResponse);

                string? title = parsedJson["title"]?.ToString();
                if (string.IsNullOrEmpty(title))
                {
                    _logger.Warning("Full book data missing title for ISBN: {ISBN}", isbn);
                    return Result<BookApiResponse>.Failure("Book data missing title");
                }

                string? key = parsedJson["key"]?.ToString();
                if (string.IsNullOrEmpty(key))
                {
                    _logger.Warning("Full book data missing key for ISBN: {ISBN}", isbn);
                    return Result<BookApiResponse>.Failure("Book data missing key");
                }

                string? subtitle = parsedJson["subtitle"]?.ToString();
                string? isbn10 = parsedJson["isbn_10"]?[0]?.ToString();
                string? isbn13 = parsedJson["isbn_13"]?[0]?.ToString();

                if (string.IsNullOrEmpty(isbn10) && string.IsNullOrEmpty(isbn13))
                {
                    string cleanedIsbn = ISBNValidator.CleanISBN(isbn);
                    if (cleanedIsbn.Length == 10)
                        isbn10 = cleanedIsbn;
                    else if (cleanedIsbn.Length == 13)
                        isbn13 = cleanedIsbn;
                }

                DateTime publishDate = DateTime.MinValue;
                string? publishDateString = parsedJson["publish_date"]?.ToString();
                if (!string.IsNullOrEmpty(publishDateString))
                {
                    _ = DateTime.TryParse(publishDateString, out publishDate);
                }

                string publisher = string.Empty;
                if (parsedJson["publishers"] is JArray publishersArray)
                {
                    publisher = publishersArray[0].ToString();
                }

                string description = string.Empty;
                if (parsedJson["works"] is JArray worksArray)
                {
                    foreach (var work in worksArray)
                    {
                        string? workKey = work["key"]?.ToString().Replace("/works/", "");

                        if (string.IsNullOrEmpty(workKey))
                            continue;

                        Result<string> descriptionResult = await FetchDescriptionByWork(workKey);
                        if (descriptionResult.IsSuccess && !string.IsNullOrEmpty(descriptionResult.Value))
                        {
                            description = descriptionResult.Value;
                            break;
                        }
                    }
                }

                List<AuthorInfo> authorInfos = [];
                if (parsedJson["authors"] is JArray authorsArray)
                {
                    foreach (var author in authorsArray)
                    {
                        string? authorKey = author["key"]?.ToString().Replace("/authors/", "");

                        if (string.IsNullOrEmpty(authorKey))
                            continue;

                        Result<AuthorInfo> authorResult = await FetchAuthor(authorKey);
                        if (authorResult.IsSuccess && authorResult.Value != null)
                        {
                            authorInfos.Add(authorResult.Value);
                        }
                    }
                }

                BitmapSource? coverImage = null;
                Result<BitmapSource> coverResult = await FetchCoverByISBN(isbn);
                if (coverResult.IsSuccess)
                {
                    coverImage = coverResult.Value;
                }

                string coverUrl = FetchCoverUrlByISBN(isbn);

                BookApiResponse book = new()
                {
                    Title = title,
                    Subtitle = subtitle,
                    Description = description,
                    Authors = authorInfos,
                    PublishDate = publishDate,
                    Publisher = publisher,
                    Isbn10 = isbn10,
                    Isbn13 = isbn13,
                    Key = key,
                    Cover = coverImage ?? CreatePlaceholderImage(),
                    CoverImageUrl = coverUrl
                };

                _logger.Information("Successfully fetched full book: {Title} with {AuthorCount} authors (ISBN: {ISBN})",
                    title, authorInfos.Count, isbn);
                return Result<BookApiResponse>.Success(book);

            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex, "Network error fetching full book ISBN: {ISBN}", isbn);
                return Result<BookApiResponse>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.Error(ex, "Invalid JSON for full book ISBN: {ISBN}", isbn);
                return Result<BookApiResponse>.Failure($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error fetching full book ISBN: {ISBN}", isbn);
                return Result<BookApiResponse>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        private static async Task<Result<AuthorInfo>> FetchAuthor(string authorKey)
        {
            try
            {
                _logger.Debug("Fetching author: {AuthorKey}", authorKey);

                var url = string.Format(_authorURLTemplate, authorKey);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warning("Author API returned {StatusCode} for key: {AuthorKey}",
                        response.StatusCode, authorKey);
                    return Result<AuthorInfo>.Failure($"Author API returned {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var parsedJson = JObject.Parse(jsonResponse);

                string? name = parsedJson["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                {
                    _logger.Warning("Author data missing name for key: {AuthorKey}", authorKey);
                    return Result<AuthorInfo>.Failure("Author data missing name");
                }

                string? bioString = parsedJson["bio"]?.ToString();
                string? bio = string.Empty;
                if (!string.IsNullOrEmpty(bioString))
                {
                    var bioObject = JObject.Parse(bioString);
                    bio = bioObject["value"]?.ToString();
                }

                AuthorInfo author = new()
                {
                    Name = name,
                    OpenLibraryKey = authorKey,
                    Bio = bio
                };

                _logger.Debug("Successfully fetched author: {Name} ({Key})", name, authorKey);
                return Result<AuthorInfo>.Success(author);
            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex, "Network error fetching author: {AuthorKey}", authorKey);
                return Result<AuthorInfo>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.Error(ex, "Invalid author JSON for key: {AuthorKey}", authorKey);
                return Result<AuthorInfo>.Failure($"Invalid author JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error fetching author: {AuthorKey}", authorKey);
                return Result<AuthorInfo>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        private static async Task<Result<string>> FetchDescriptionByWork(string workKey)
        {
            try
            {
                var url = string.Format(_workURLTemplate, workKey);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<string>.Failure($"Work API returned {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var parsedJson = JObject.Parse(jsonResponse);

                string? descriptionString = parsedJson["description"]?.ToString();

                if (string.IsNullOrEmpty(descriptionString))
                {
                    return Result<string>.Failure("Work data missing description");
                }

                var descriptionJson = JObject.Parse(descriptionString);
                string? description = descriptionJson["value"]?.ToString();

                if (string.IsNullOrEmpty(description))
                {
                    return Result<string>.Failure("Work description is empty");
                }

                return Result<string>.Success(description);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to fetch description for work: {WorkKey}", workKey);
                return Result<string>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        public static string FetchCoverUrlByISBN(string isbn)
        {
            return string.Format(_coverUrlTemplate, isbn);
        }

        public static async Task<Result<BitmapSource>> FetchCoverByISBN(string isbn)
        {
            if (_coverCache.TryGetValue(isbn, out var cachedCover))
            {
                _logger.Debug("Cover cache hit for ISBN: {ISBN}", isbn);
                return Result<BitmapSource>.Success(cachedCover);
            }

            try
            {
                _logger.Debug("Fetching cover for ISBN: {ISBN}", isbn);

                var url = string.Format(_coverUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Debug("Cover API returned {StatusCode} for ISBN: {ISBN}", response.StatusCode, isbn);
                    return Result<BitmapSource>.Failure($"Cover API returned {response.StatusCode}");
                }

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                if (imageBytes.Length < 500)
                {
                    _logger.Debug("Cover image too small for ISBN: {ISBN}, using placeholder", isbn);
                    var placeholder = CreatePlaceholderImage();
                    _coverCache[isbn] = placeholder;
                    return Result<BitmapSource>.Success(placeholder);
                }

                BitmapImage bmImg = new();

                using (MemoryStream memoryStream = new(imageBytes))
                {
                    bmImg.BeginInit();
                    bmImg.CacheOption = BitmapCacheOption.OnLoad;
                    bmImg.StreamSource = memoryStream;
                    bmImg.EndInit();
                }

                bmImg.Freeze();

                if (bmImg.PixelWidth < 10 || bmImg.PixelHeight < 10)
                {
                    _logger.Debug("Cover dimensions too small for ISBN: {ISBN}, using placeholder", isbn);
                    var placeholder = CreatePlaceholderImage();
                    _coverCache[isbn] = placeholder;
                    return Result<BitmapSource>.Success(placeholder);
                }

                _coverCache[isbn] = bmImg;
                _logger.Debug("Successfully fetched and cached cover for ISBN: {ISBN}", isbn);

                return Result<BitmapSource>.Success(bmImg);
            }
            catch (HttpRequestException ex)
            {
                _logger.Warning(ex, "Network error fetching cover for ISBN: {ISBN}", isbn);
                return Result<BitmapSource>.Failure($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error fetching cover for ISBN: {ISBN}", isbn);
                return Result<BitmapSource>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        public static BitmapSource CreatePlaceholderImage()
        {
            if (_placeholderImage != null)
                return _placeholderImage;

            try
            {
                int width = 128;
                int height = 192;

                var visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen())
                {
                    context.DrawRectangle(Brushes.LightGray, null, new System.Windows.Rect(0, 0, width, height));
                    var text = new FormattedText(
                        "No Cover",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        24,
                        Brushes.DarkGray,
                        1.0);
                    context.DrawText(text, new System.Windows.Point(
                        (width - text.Width) / 2,
                        (height - text.Height) / 2));
                }

                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);
                renderBitmap.Freeze();

                _placeholderImage = renderBitmap;
                return _placeholderImage;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create placeholder image");
                throw;
            }
        }

        public static string GetUserFriendlyError(string technicalError)
        {
            if (string.IsNullOrEmpty(technicalError))
                return "Unknown error occurred";

            if (technicalError.Contains("API returned 404") || technicalError.Contains("NotFound"))
                return "This book wasn't found in our online database. You can add it manually instead.";

            if (technicalError.Contains("API returned 503") || technicalError.Contains("Service Unavailable"))
                return "The book database is temporarily unavailable. Please try again in a few minutes.";

            if (technicalError.Contains("Network error") || technicalError.Contains("timeout"))
                return "Connection problem detected. Check your internet connection and try again.";

            if (technicalError.Contains("Invalid JSON"))
                return "Received corrupted data from the server. This might be a temporary issue.";

            if (technicalError.Contains("missing title"))
                return "The book data is incomplete. Try adding this book manually.";

            return "Unable to retrieve book information. You can try again or add the book manually.";
        }
    }

    internal class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }

        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default, error);
    }
}