using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MyAthenaeio.Models;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using MyAthenaeio.Data;


namespace MyAthenaeio.Services
{
    internal static class BookAPIService
    {
        private static HttpClient _bookClient = new();
        private const string _bookISBNUrlTemplate = "https://openlibrary.org/isbn/{0}.json";
        private const string _coverUrlTemplate = "https://covers.openlibrary.org/b/isbn/{0}-M.jpg";
        private const string _authorURLTemplate = "https://openlibrary.org/authors/{0}.json";
        private const string _workURLTemplate = "https://openlibrary.org/works/{0}.json";
        private static BitmapImage? _placeholderImage;

        private static Dictionary<string, BitmapImage> _coverCache = new();

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

                // Fetch cover
                BitmapImage? cover = null;
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

                // Parse publish data
                DateTime publishDate = DateTime.MinValue;
                string? publishDateString = parsedJson["publish_date"]?.ToString();
                if (!string.IsNullOrEmpty(publishDateString))
                {
                    _ = DateTime.TryParse(publishDateString, out publishDate);
                }

                // Get the work and description
                // There should technically only be one work per ISBN
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
                List<Author> authors = new();
                if (parsedJson["authors"] is JArray authorsArray)
                {
                    foreach (var author in authorsArray)
                    {
                        string? authorKey = author["key"]?.ToString().Replace("/authors/", "");

                        if (string.IsNullOrEmpty(authorKey))
                            continue;

                        // Fetch author name
                        Result<Author> authorResult = await FetchAuthor(authorKey);
                        if (authorResult.IsSuccess && authorResult.Value != null)
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

                BookApiResponse book = new()
                {
                    Title = title,
                    Subtitle = subtitle,
                    Description = description,
                    Authors = authors,
                    PublishDate = publishDate,
                    Isbn10 = isbn10,
                    Isbn13 = isbn13,
                    Key = key,
                    Cover = coverImage ?? CreatePlaceholderImage()
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

        private static async Task<Result<Author>> FetchAuthor(string authorKey)
        {
            try
            {
                var url = string.Format(_authorURLTemplate, authorKey);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<Author>.Failure($"Author API returned {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var parsedJson = JObject.Parse(jsonResponse);

                string? name = parsedJson["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                {
                    return Result<Author>.Failure("Author data missing name");
                }

                string? bioString = parsedJson["bio"]?.ToString();
                string? bio = string.Empty;
                if (!string.IsNullOrEmpty(bioString))
                {
                    var bioObject = JObject.Parse(bioString);
                    bio = bioObject["value"]?.ToString();
                }

                Author author = new()
                {
                    Name = name,
                    Bio = bio
                };

                return Result<Author>.Success(author);
            }
            catch (HttpRequestException ex)
            {
                return Result<Author>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<Author>.Failure($"Invalid author JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<Author>.Failure($"Unexpected error: {ex.Message}");
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

        public static async Task<Result<BitmapImage>> FetchCoverByISBN(string isbn)
        {
            // Check cache first
            if (_coverCache.TryGetValue(isbn, out var cachedCover))
            {
                return Result<BitmapImage>.Success(cachedCover);
            }

            try
            {
                var url = string.Format(_coverUrlTemplate, isbn);
                using HttpResponseMessage response = await _bookClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<BitmapImage>.Failure($"Cover API returned {response.StatusCode}");
                }


                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                // Check if image data is likely too small
                if (imageBytes.Length < 500)
                {
                    var placeholder = CreatePlaceholderImage();
                    _coverCache[isbn] = placeholder;
                    return Result<BitmapImage>.Success(placeholder);
                }

                BitmapImage bmImg = new();

                using (MemoryStream memoryStream = new(imageBytes))
                {
                    bmImg.BeginInit();
                    bmImg.CacheOption = BitmapCacheOption.OnLoad;
                    bmImg.StreamSource = memoryStream;
                    bmImg.EndInit();
                    bmImg.Freeze();
                }

                // Check pixel dimensions
                if (bmImg.PixelWidth < 10 || bmImg.PixelHeight < 10)
                {
                    var placeholder = CreatePlaceholderImage();
                    _coverCache[isbn] = placeholder;
                    return Result<BitmapImage>.Success(placeholder);
                }

                // Cache the result
                _coverCache[isbn] = bmImg;

                return Result<BitmapImage>.Success(bmImg);
            }
            catch (HttpRequestException ex)
            {
                return Result<BitmapImage>.Failure($"Network error: {ex.Message}");
            }
        }

        public static BitmapImage CreatePlaceholderImage()
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
