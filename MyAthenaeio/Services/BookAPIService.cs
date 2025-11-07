using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MyAthenaeio.Models;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;


namespace MyAthenaeio.Services
{
    internal static class BookAPIService
    {
        private static HttpClient _bookClient = new();
        private const string _bookISBNUrlTemplate = "https://openlibrary.org/isbn/{0}.json";
        private const string _coverUrlTemplate = "https://covers.openlibrary.org/b/isbn/{0}-M.jpg";
        private const string _authorURLTemplate = "https://openlibrary.org/authors/{0}.json";
        private static BitmapImage? _placeholderImage;

        public static async Task<Result<Book>> FetchBookByISBN(string isbn)
        {
            try
            {
                var url = string.Format(_bookISBNUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<Book>.Failure($"API returned {response.StatusCode}");
                }

                // Process the JSON
                var jsonResponse = await response.Content.ReadAsStringAsync();

                JObject parsedJson = JObject.Parse(jsonResponse);

                // Required fields
                string? title = parsedJson["title"]?.ToString();
                if (string.IsNullOrEmpty(title))
                {
                    return Result<Book>.Failure("Book data missing title");
                }

                string? key = parsedJson["key"]?.ToString();
                if (string.IsNullOrEmpty(key))
                {
                    return Result<Book>.Failure("Book data missing key");
                }

                // Optional fields
                string? subtitle = parsedJson["subtitle"]?.ToString();
                string? isbn10 = parsedJson["isbn_10"]?[0]?.ToString();
                string? isbn13 = parsedJson["isbn_13"]?[0]?.ToString();

                // Parse publish data
                DateTime publishDate = DateTime.MinValue;
                string? publishDateString = parsedJson["publish_date"]?.ToString();
                if (!string.IsNullOrEmpty(publishDateString))
                {
                    _ = DateTime.TryParse(publishDateString, out publishDate);
                }

                // Authors array
                List<string> authors = new();
                if (parsedJson["authors"] is JArray authorsArray)
                {
                    foreach (var author in authorsArray)
                    {
                        string? authorKey = author["key"]?.ToString().Replace("/authors/", "");

                        if (string.IsNullOrEmpty(authorKey))
                            continue;

                        // Fetch author name
                        Result<string> authorResult = await FetchAuthor(authorKey);
                        if (authorResult.IsSuccess && !string.IsNullOrEmpty(authorResult.Value))
                        {
                            authors.Add(authorResult.Value);
                        }

                        // Not critical if author fetch fails
                    }
                }

                // Fetch cover
                BitmapImage? coverImage = null;
                Result<BitmapImage> coverResult = await FetchCoverByISBN(isbn);
                if (coverResult.IsSuccess)
                {
                    coverImage = coverResult.Value;
                }

                Book book = new()
                {
                    Title = title,
                    Subtitle = subtitle,
                    Authors = authors,
                    PublishDate = publishDate,
                    Isbn10 = isbn10,
                    Isbn13 = isbn13,
                    Key = key,
                    Cover = coverImage ?? CreatePlaceholderImage()
                };

                return Result<Book>.Success(book);

            }
            catch (HttpRequestException ex)
            {
                return Result<Book>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<Book>.Failure($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<Book>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        private static async Task<Result<string>> FetchAuthor(string authorKey)
        {
            try
            {
                var url = string.Format(_authorURLTemplate, authorKey);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<string>.Failure($"Author API returned {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var parsedJson = JObject.Parse(jsonResponse);

                string? name = parsedJson["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                {
                    return Result<string>.Failure("Author data missing name");
                }

                return Result<string>.Success(name);
            }
            catch (HttpRequestException ex)
            {
                return Result<string>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<string>.Failure($"Invalid author JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        private static async Task<Result<BitmapImage>> FetchCoverByISBN(string isbn)
        {
            try
            {
                var url = string.Format(_coverUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                    BitmapImage bmImg = new();

                    using (MemoryStream memoryStream = new(imageBytes))
                    {
                        bmImg.BeginInit();
                        bmImg.CacheOption = BitmapCacheOption.OnLoad;
                        bmImg.StreamSource = memoryStream;
                        bmImg.EndInit();
                    }

                    return Result<BitmapImage>.Success(bmImg);
                }

                return Result<BitmapImage>.Failure($"API returned {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return Result<BitmapImage>.Failure($"Network error: {ex.Message}");
            }
        }

        private static BitmapImage CreatePlaceholderImage()
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
                    16,
                    Brushes.DarkGray,
                    1.0);

                context.DrawText(text, new System.Windows.Point(
                    (width - text.Width) / 2,
                    (height - text.Height) / 2));
            }

            var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            var bitmapImage = new BitmapImage();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Must freeze for caching

            _placeholderImage = bitmapImage;
            return _placeholderImage;
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
