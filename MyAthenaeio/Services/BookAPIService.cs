using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using MyAthenaeio.Utils;
using MyAthenaeio.Models.DTOs;

namespace MyAthenaeio.Services
{
    internal static class BookApiService
    {
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
                var url = string.Format(_bookISBNUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<BookApiResponse>.Failure($"API returned {response.StatusCode}");
                }

                // Process the JSON
                var jsonResponse = await response.Content.ReadAsStringAsync();

                JObject parsedJson = JObject.Parse(jsonResponse);

                // Required fields
                string? title = parsedJson["title"]?.ToString();
                if (string.IsNullOrEmpty(title))
                {
                    return Result<BookApiResponse>.Failure("Book data missing title");
                }

                // Optional fields
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

                // Fetch cover
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

                return Result<BookApiResponse>.Success(book);

            }
            catch (HttpRequestException ex)
            {
                return Result<BookApiResponse>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<BookApiResponse>.Failure($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<BookApiResponse>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        public static async Task<Result<BookApiResponse>> FetchFullBookByISBN(string isbn)
        {
            try
            {
                var url = string.Format(_bookISBNUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<BookApiResponse>.Failure($"API returned {response.StatusCode}");
                }

                // Process the JSON
                var jsonResponse = await response.Content.ReadAsStringAsync();

                JObject parsedJson = JObject.Parse(jsonResponse);

                // Required fields
                string? title = parsedJson["title"]?.ToString();
                if (string.IsNullOrEmpty(title))
                {
                    return Result<BookApiResponse>.Failure("Book data missing title");
                }

                string? key = parsedJson["key"]?.ToString();
                if (string.IsNullOrEmpty(key))
                {
                    return Result<BookApiResponse>.Failure("Book data missing key");
                }

                // Optional fields
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

                // Parse publish data
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

                // Get the work and description
                string description = string.Empty;
                if (parsedJson["works"] is JArray worksArray)
                {
                    foreach (var work in worksArray)
                    {
                        string? workKey = work["key"]?.ToString().Replace("/works/", "");

                        if (string.IsNullOrEmpty(workKey))
                            continue;

                        // Fetch description
                        Result<string> descriptionResult = await FetchDescriptionByWork(workKey);
                        if (descriptionResult.IsSuccess && !string.IsNullOrEmpty(descriptionResult.Value))
                        {
                            description = descriptionResult.Value;
                            break;
                        }
                    }
                }

                // Authors array
                List<AuthorInfo> authorInfos = [];
                if (parsedJson["authors"] is JArray authorsArray)
                {
                    foreach (var author in authorsArray)
                    {
                        string? authorKey = author["key"]?.ToString().Replace("/authors/", "");

                        if (string.IsNullOrEmpty(authorKey))
                            continue;

                        // Fetch author name
                        Result<AuthorInfo> authorResult = await FetchAuthor(authorKey);
                        if (authorResult.IsSuccess && authorResult.Value != null)
                        {
                            authorInfos.Add(authorResult.Value);
                        }
                    }
                }

                // Fetch cover
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

                return Result<BookApiResponse>.Success(book);

            }
            catch (HttpRequestException ex)
            {
                return Result<BookApiResponse>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<BookApiResponse>.Failure($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<BookApiResponse>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        private static async Task<Result<AuthorInfo>> FetchAuthor(string authorKey)
        {
            try
            {
                var url = string.Format(_authorURLTemplate, authorKey);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<AuthorInfo>.Failure($"Author API returned {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var parsedJson = JObject.Parse(jsonResponse);

                string? name = parsedJson["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                {
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

                return Result<AuthorInfo>.Success(author);
            }
            catch (HttpRequestException ex)
            {
                return Result<AuthorInfo>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<AuthorInfo>.Failure($"Invalid author JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
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
            catch (HttpRequestException ex)
            {
                return Result<string>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<string>.Failure($"Invalid work JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        public static string FetchCoverUrlByISBN(string isbn)
        {
            return string.Format(_coverUrlTemplate, isbn);
        }

        public static async Task<Result<BitmapSource>> FetchCoverByISBN(string isbn)
        {
            // Check cache first
            if (_coverCache.TryGetValue(isbn, out var cachedCover))
            {
                return Result<BitmapSource>.Success(cachedCover);
            }

            try
            {
                var url = string.Format(_coverUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<BitmapSource>.Failure($"Cover API returned {response.StatusCode}");
                }

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                // Check if image data is likely too small
                if (imageBytes.Length < 500)
                {
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

                // Freeze after the using block when stream is closed
                bmImg.Freeze();

                // Check pixel dimensions
                if (bmImg.PixelWidth < 10 || bmImg.PixelHeight < 10)
                {
                    var placeholder = CreatePlaceholderImage();
                    _coverCache[isbn] = placeholder;
                    return Result<BitmapSource>.Success(placeholder);
                }

                // Cache the result (now frozen)
                _coverCache[isbn] = bmImg;

                return Result<BitmapSource>.Success(bmImg);
            }
            catch (HttpRequestException ex)
            {
                return Result<BitmapSource>.Failure($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<BitmapSource>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        public static BitmapSource CreatePlaceholderImage()
        {
            if (_placeholderImage != null)
                return _placeholderImage;

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