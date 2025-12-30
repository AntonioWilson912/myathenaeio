using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using MyAthenaeio.Services;

namespace MyAthenaeio.Converters
{
    public class BookCoverConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(url, UriKind.Absolute);
                    image.DecodePixelWidth = 100; // Thumbnail size for performance
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch
                {
                    // If loading fails, return placeholder
                    return BookApiService.CreatePlaceholderImage();
                }
            }

            // No URL - return placeholder
            return BookApiService.CreatePlaceholderImage();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}