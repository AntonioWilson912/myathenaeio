using MyAthenaeio.Models.Settings;
using System.IO;
using System.Text.Json;

namespace MyAthenaeio.Services
{
    public class SettingsService
    {
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

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            Settings = newSettings;
            SaveSettings();
        }
    }
}