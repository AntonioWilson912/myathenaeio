using MyAthenaeio.Data;
using System.Diagnostics;
using System.IO;

namespace MyAthenaeio.Services
{
    public class BackupService
    {
        private static readonly string BackupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "myAthenaeio", "Backups"
        );

        public static async Task CheckAndCreateAutomaticBackupAsync()
        {
            try
            {
                Directory.CreateDirectory(BackupFolder);

                var lastBackup = GetMostRecentBackup();

                // Create backup if none exists or if last backup is older than 7 days
                if (lastBackup == null || File.GetCreationTime(lastBackup) < DateTime.Now.AddDays(-7))
                {
                    await CreateAutomaticBackupAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during automatic backup check/creation: {ex.Message}");
            }
        }

        public static List<string> GetAvailableBackups()
        {
            if (!Directory.Exists(BackupFolder))
                return [];

            return [.. Directory.GetFiles(BackupFolder, "library_backup_*.db").OrderByDescending(f => File.GetCreationTime(f))];
        }

        private static async Task CreateAutomaticBackupAsync()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFilePath = Path.Combine(BackupFolder, $"library_backup_{timestamp}.db");

            File.Copy(AppDbContext.DbPath, backupFilePath);

            // Keep only the latest 5 backups
            CleanupOldBackups(keepCount: 5);

            Debug.WriteLine($"Automatic backup created at {backupFilePath}");
        }

        private static string? GetMostRecentBackup()
        {
            var backupFiles = Directory.GetFiles(BackupFolder, "library_backup_*.db");
            return backupFiles
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .FirstOrDefault()?.FullName;
        }

        private static void CleanupOldBackups(int keepCount)
        {
            var backupFiles = Directory.GetFiles(BackupFolder, "library_backup_*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(keepCount)
                .ToList();

            foreach (var oldBackup in backupFiles)
            {
                try
                {
                    oldBackup.Delete();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting backup file {oldBackup.FullName}: {ex.Message}");
                }
            }
        }
    }
}
