using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
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
                    image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    image.EndInit();

                    // Freeze on UI thread before returning
                    if (image.CanFreeze)
                        image.Freeze();

                    return image;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading cover: {ex.Message}");
                    // Return placeholder on error
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