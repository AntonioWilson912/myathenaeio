using MyAthenaeio.Models.Settings;
using Serilog;
using System.IO;
using System.Text.Json;

namespace MyAthenaeio.Services
{
    public class SettingsService
    {
        private static readonly ILogger _logger = Log.ForContext<SettingsService>();
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "myAthenaeio",
            "settings.json"
        );

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            Settings = LoadSettings();
        }

        private static AppSettings LoadSettings()
        {
            try
            {
                _logger.Debug("Loading app settings from {SettingsFilePath}", SettingsFilePath);
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load app settings.");
            }

            return new AppSettings();
        }

        public void SaveSettings()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(Settings, SerializerOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ffailed to save app settings.");
            }
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            Settings = newSettings;
            SaveSettings();
        }
    }
}